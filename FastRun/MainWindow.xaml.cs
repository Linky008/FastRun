using GlobalHotKeyDemo;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FastRun
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private static MainWindow m_Instance;
        private IntPtr handle = new IntPtr();
        Dictionary<string, string> dcCommands = new Dictionary<string, string>();        
        NotifyIcon notifyIcon;
        WindowState ws;
        WindowState wsl;

        /// <summary>
        /// 记录快捷键注册项的唯一标识符
        /// </summary>
        private Dictionary<EHotKeySetting, int> m_HotKeySettings = new Dictionary<EHotKeySetting, int>();

        public MainWindow()
        {
            InitializeComponent();
            m_Instance = this;
            //显示托盘。
            icon();
            //保证窗体显示在上方。
            wsl = WindowState;
            LoadCommands();
        }

        /// <summary>
        /// 窗体加载完成后事件处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HotKeySettingsManager.Instance.RegisterGlobalHotKeyEvent += Instance_RegisterGlobalHotKeyEvent;
        }

        /// <summary>
        /// 通知注册系统快捷键事件处理函数
        /// </summary>
        /// <param name="hotKeyModelList"></param>
        /// <returns></returns>
        private bool Instance_RegisterGlobalHotKeyEvent(ObservableCollection<HotKeyModel> hotKeyModelList)
        {
            return InitHotKey(hotKeyModelList);
        }

        private void txtCommand_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    string command = txtCommand.Text.Trim();
                    string commandvalue = dcCommands[command];

                    string[] args = commandvalue.Split('\\');

                    if (args[args.Length - 1].Contains("."))
                    {
                        System.Diagnostics.Process.Start(commandvalue);
                    }
                    else
                    {
                        System.Diagnostics.Process.Start("explorer", commandvalue);
                    }

                    //string path1 = @"d:\log.txt";  //打开D盘下的log.txt文件
                    //System.Diagnostics.Process.Start(path1);
                    //string path2 = @"d:\test";  //调用资源管理器，打开D盘下的test文件夹
                }
                catch
                {
                    System.Windows.MessageBox.Show("尚未收录，请添加");
                    System.Diagnostics.Process.Start("config.txt");
                }

            }
        }

        public void LoadCommands()
        {
            FileStream fileStream = File.OpenRead("config.txt");
            StreamReader reader = new StreamReader(fileStream);

            string line = null;

            while ((line = reader.ReadLine()) != null)
            {
                string[] commands = line.Split('|');
                dcCommands.Add(commands[0], commands[1]);
            }
        }

        private void txtCommand_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            lbContent.Content = "";
            var keyVal = dcCommands.Where(c => c.Key.Contains(txtCommand.Text.Trim()));
            foreach (var kv in keyVal)
            {
                lbContent.Content += kv.Key + "|" + kv.Value;
                lbContent.Content += "\n\r";
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //e.Cancel = true;
            //this.WindowState = WindowState.Minimized;
            //注销热键
            //UnregisterHotKey(handle, 1);
        }

        private void icon()
        {
            this.notifyIcon = new NotifyIcon();
            //this.notifyIcon.BalloonTipText = "Hello, 文件监视器"; //设置程序启动时显示的文本
            this.notifyIcon.Text = "文件监视器";//最小化到托盘时，鼠标点击时显示的文本
            this.notifyIcon.Icon = new System.Drawing.Icon("..\\..\\Images\\Icons\\Downloads.ico");//程序图标
            this.notifyIcon.Visible = true;
            notifyIcon.MouseDoubleClick += OnNotifyIconDoubleClick;
            //this.notifyIcon.ShowBalloonTip(1000);
        }

        private void OnNotifyIconDoubleClick(object sender, EventArgs e)
        {
            this.Show();
            WindowState = wsl;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            ws = WindowState;
            if (ws == WindowState.Minimized)
            {
                this.Hide();
            }
        }

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // 获取窗体句柄
            handle = new WindowInteropHelper(this).Handle;
           

            HwndSource source = HwndSource.FromHwnd(handle);
            if (source != null)
            {
                source.AddHook(WndProc);
            }
        }

        /// <summary>
        /// 所有控件初始化完成后调用
        /// </summary>
        /// <param name="e"></param>
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            // 注册热键
            InitHotKey();
        }

        /// <summary>
        /// 初始化注册快捷键
        /// </summary>
        /// <param name="hotKeyModelList">待注册热键的项</param>
        /// <returns>true:保存快捷键的值；false:弹出设置窗体</returns>
        private bool InitHotKey(ObservableCollection<HotKeyModel> hotKeyModelList = null)
        {
            var list = hotKeyModelList ?? HotKeySettingsManager.Instance.LoadDefaultHotKey();
            // 注册全局快捷键
            string failList = HotKeyHelper.RegisterGlobalHotKey(list, handle, out m_HotKeySettings);
            if (string.IsNullOrEmpty(failList))
                return true;
            //MessageBoxResult mbResult = System.Windows.MessageBox.Show(string.Format("无法注册下列快捷键\n\r{0}是否要改变这些快捷键？", failList), "提示", MessageBoxButton.YesNo);
            // 弹出热键设置窗体
            var win = MainWindow.CreateInstance();
            //if (mbResult == MessageBoxResult.Yes)
            //{
            //    if (!win.IsVisible)
            //    {
            //        win.ShowDialog();
            //    }
            //    else
            //    {
            //        win.Activate();
            //    }
            //    return false;
            //}
            return true;
        }

        /// <summary>
        /// 创建系统参数设置窗体实例
        /// </summary>
        /// <returns></returns>
        public static MainWindow CreateInstance()
        {
            return m_Instance ?? (m_Instance = new MainWindow());
        }

        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handle)
        {            
            var hotkeySetting = new EHotKeySetting();
            if (msg== HotKeyManager.WM_HOTKEY)
            {
                int sid = wParam.ToInt32();
                //全局快捷键要执行的命令
                if (sid == m_HotKeySettings[EHotKeySetting.调用])
                {
                    hotkeySetting = EHotKeySetting.调用;
                    //TODO 执行全屏操作

                    var win = MainWindow.CreateInstance();
                    win.Show();
                    win.WindowState = WindowState.Normal;
                }

                handle=true;

            }
            return IntPtr.Zero;
        }
    }
}
