#define DEBUG
#undef DEBUG

using ColorPicker.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NETColorConverter;
using System.Text.RegularExpressions;
using ColorConverter = NETColorConverter.ColorConverter;
using Rectangle = System.Windows.Shapes.Rectangle;
using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
using System.Threading;
using System.Drawing.Drawing2D;
using LinearGradientBrush = System.Windows.Media.LinearGradientBrush;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ColorPicker
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\ColorPicker";
        private class HSVCOLOR
        {
            public double HSelectorX { get; }
            public double HSelectorY { get; }
            public double SLSelectorX { get; }
            public double SLSelectorY { get; }
            public double H { get; }
            public double S { get; }
            public double V { get; }
            public HSVCOLOR(MainWindow mainwindow,double H,double S,double V)  
            {
                this.H = H;this.S = S;this.V = V;
                //H = H != 360 ? H : 0;
                HSelectorX = (mainwindow.HSelector.ActualWidth - mainwindow.rectangle.Width) / 2.0;
                HSelectorY = (1.0 - H / 360.0) * mainwindow.HSelector.ActualHeight - mainwindow.rectangle.Height / 2.0;
                SLSelectorX = S * mainwindow.SLSelector.ActualWidth - mainwindow.ellipse.Width / 2.0;
                SLSelectorY = ((1.0 - V) * mainwindow.SLSelector.ActualHeight) - mainwindow.ellipse.Height / 2.0;
            }
            public HSVCOLOR(MainWindow mainwindow)
            {
                HSelectorX = (mainwindow.HSelector.ActualWidth - mainwindow.rectangle.Width) / 2.0;
                HSelectorY = Canvas.GetTop(mainwindow.rectangle) + mainwindow.rectangle.Height / 2.0;
                SLSelectorX = Canvas.GetLeft(mainwindow.ellipse) + mainwindow.ellipse.Width / 2.0;
                SLSelectorY = Canvas.GetTop(mainwindow.ellipse) + mainwindow.ellipse.Height / 2.0;
                H = 360.0 * (1.0 - HSelectorY / mainwindow.HSelector.ActualHeight);
                S = SLSelectorX / mainwindow.SLSelector.ActualWidth;
                V = 1.0 - SLSelectorY / mainwindow.HSelector.ActualHeight;

            }
        }

        private readonly Ellipse ellipse = new Ellipse
        {
            Height = 16,
            Width = 16,
            Stroke = new SolidColorBrush(Colors.Black),
            StrokeThickness = 2
        };
        private readonly Rectangle rectangle = new Rectangle
        {
            Height = 10,
            Width = 35,
            Fill = new SolidColorBrush(Color.FromArgb(0,255,255,255)),
            Stroke = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
            StrokeThickness = 2,
            Cursor = Cursors.Hand
        };
        private HSVCOLOR loadcolor;
        public MainWindow()  
        {
            InitializeComponent();
        }
        private string CursorFile, CursorFile2, SettingFile;
        //private delegate void CONVERTER(double A, double B, double C, double D, double E, double F);
        #if DEBUG
            private const int linewidth = 5;
            private const int pixelsize = 10;
        #else
        private const int linewidth = 2;
            private const int pixelsize = 3;
        #endif

        private TextBox[] TextBoxList;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {         
            TextBoxList = new TextBox[]
            {
                ParaR_RGB,ParaG_RGB,ParaB_RGB,
                ParaH_HSV,ParaS_HSV,ParaV_HSV,
                ParaH_HSL,ParaS_HSL,ParaL_HSL,
                Para_HEX
            };
            CursorFile = AppDataPath + "\\circle.cur";
            CursorFile2 = AppDataPath + "\\circle_white.cur";
            SettingFile = AppDataPath + "\\config.json";
            if (!Directory.Exists(AppDataPath))
            {
                Directory.CreateDirectory(AppDataPath);
            }
            if (!File.Exists(CursorFile))
            {
                FileStream fs = new FileStream(CursorFile,FileMode.Create);
                byte[] data = Properties.Resources.circle;
                foreach (byte bytes in data) fs.WriteByte(bytes);
                fs.Flush();fs.Close();
            }
            if (!File.Exists(CursorFile2))
            {
                FileStream fs = new FileStream(CursorFile2, FileMode.Create);
                byte[] data = Properties.Resources.circle_white;
                foreach (byte bytes in data) fs.WriteByte(bytes);
                fs.Flush(); fs.Close();
            }
            if (!File.Exists(SettingFile))
            {
                StringWriter stringwriter = new StringWriter();
                JsonTextWriter jtw = new JsonTextWriter(stringwriter);
                jtw.Formatting = Newtonsoft.Json.Formatting.Indented;
                jtw.WriteStartObject();
                jtw.WritePropertyName("window");
                jtw.WriteStartObject();
                jtw.WritePropertyName("left");
                jtw.WriteValue(this.Left);
                jtw.WritePropertyName("top");
                jtw.WriteValue(this.Top);
                jtw.WriteEndObject();
                jtw.WritePropertyName("color");
                jtw.WriteStartObject();
                jtw.WritePropertyName("H");
                jtw.WriteValue(0.0);
                jtw.WritePropertyName("S");
                jtw.WriteValue(0.0);
                jtw.WritePropertyName("V");
                jtw.WriteValue(0.0);
                jtw.WriteEndObject();
                jtw.WriteEndObject();      
                StreamWriter sw = new StreamWriter(SettingFile);
                sw.Write(stringwriter);
                sw.Flush();
                sw.Close();
            }
            StreamReader sr = new StreamReader(SettingFile);
            JsonTextReader jtr = new JsonTextReader(sr);
            JObject jobj = (JObject)JToken.ReadFrom(jtr);
            JToken jtwindow = jobj["window"];
            JToken jtcolor = jobj["color"];
            this.Top = (double)jtwindow["top"];
            this.Left = (double)jtwindow["left"];
            double H = (double)jtcolor["H"];
            double S = (double)jtcolor["S"];
            double V = (double)jtcolor["V"];
            sr.Close();
            this.Visibility = Visibility.Visible;
            HSVCOLOR hsvcolor = new HSVCOLOR(this, H, S, V);
            HSelect_paint(); 
            Canvas.SetLeft(rectangle, hsvcolor.HSelectorX);
            Canvas.SetTop(rectangle, hsvcolor.HSelectorY);
            if (rectangle.Parent == null) HSelector.Children.Add(rectangle);
            SLSelect_paint();
            SLSelector.Cursor = new Cursor(CursorFile);
            Canvas.SetLeft(ellipse, hsvcolor.SLSelectorX);
            Canvas.SetTop(ellipse, hsvcolor.SLSelectorY);
            if (ellipse.Parent == null) SLSelector.Children.Add(ellipse);
            Color color = hsvcolor.V > 0.5 ? Colors.Black : Colors.LightGray;
            ellipse.Stroke = new SolidColorBrush(color);
            HSVColorChanged(hsvcolor);
            //Textbox_TextChanged_Add();
        }

        private void Straw_Click(object sender, RoutedEventArgs e)
        {
            var cdg = new System.Windows.Forms.ColorDialog
            {
                AllowFullOpen = false
            };
            SolidColorBrush brush = Color_Present.Background as SolidColorBrush;
            cdg.Color=System.Drawing.Color.FromArgb(brush.Color.R,brush.Color.G,brush.Color.B);
            if (cdg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Color color = Color.FromRgb(cdg.Color.R, cdg.Color.G, cdg.Color.B);
                RGBColorChanged(color, sender);
                //Color_Before.Background = Color_Present.Background;
                //Color_Present.Background = new SolidColorBrush(color);
            }
        }
        private void SLSelector_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ellipse.Parent != null) 
            {
                ((Canvas)sender).Children.Remove(ellipse);
                MouseButtonPressed = true;
            }
        }

        private void SLSelector_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Canvas canvas = (Canvas)sender;
            Point point = e.GetPosition(canvas);
            if (point.X >= 0 && point.X <= canvas.ActualWidth)
                Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
            if(point.Y >= 0 && point.Y <= canvas.ActualHeight)
                Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);
            if (ellipse.Parent == null) canvas.Children.Add(ellipse);
            MouseButtonPressed = false;
            SLSelector_Changed(sender);
        }

        private void SLSelector_MouseLeave(object sender, MouseEventArgs e)
        {
            if (MouseButtonPressed)
            {
                Canvas canvas = (Canvas)sender;
                Point point = e.GetPosition(canvas);
                bool isXfeasible = point.X >= 0 && point.X <= canvas.ActualWidth;
                bool isYfeasible = point.Y >= 0 && point.Y <= canvas.ActualHeight;
                if (isXfeasible && isYfeasible)
                {
                    Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
                    Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);
                }
                else if (!isXfeasible && isYfeasible)
                {
                    if (point.X < 0)
                    {
                        Canvas.SetLeft(ellipse, 0 - ellipse.Width / 2);
                        Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);
                    }
                    else
                    {
                        Canvas.SetLeft(ellipse, canvas.ActualWidth - ellipse.Width / 2);
                        Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);
                    }
                }
                else if(isXfeasible && !isYfeasible)
                {
                    if (point.Y < 0)
                    {
                        Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
                        Canvas.SetTop(ellipse, 0 - ellipse.Height / 2);
                    }
                    else
                    {
                        Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
                        Canvas.SetTop(ellipse, canvas.ActualHeight - ellipse.Height / 2);
                    }
                }
                else
                {
                    if (point.X < 0 && point.Y < 0)
                    {
                        Canvas.SetLeft(ellipse, 0 - ellipse.Width / 2);
                        Canvas.SetTop(ellipse, 0 - ellipse.Height / 2);
                    }
                    else if (point.X > 0 && point.Y < 0)
                    {
                        Canvas.SetLeft(ellipse, canvas.ActualWidth - ellipse.Width / 2);
                        Canvas.SetTop(ellipse, 0 - ellipse.Height / 2);
                    }
                    else if(point.X < 0 && point.Y > 0)
                    {
                        Canvas.SetLeft(ellipse, 0 - ellipse.Width / 2);
                        Canvas.SetTop(ellipse, canvas.ActualHeight - ellipse.Height / 2);
                    }
                    else if (point.X > 0 && point.Y > 0)
                    {
                        Canvas.SetLeft(ellipse, canvas.ActualWidth - ellipse.Width / 2);
                        Canvas.SetTop(ellipse, canvas.ActualHeight - ellipse.Height / 2);
                    }
                }
                if (ellipse.Parent == null) canvas.Children.Add(ellipse);
                MouseButtonPressed = false;
                SLSelector_Changed(sender);
            }
        }

        private bool MouseButtonPressed = false;
        private void HSelector_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Canvas canvas = (Canvas)sender;
            Point point = e.GetPosition(canvas);
            ChangeSlider(point.Y);
            if (rectangle.Parent == null) canvas.Children.Add(rectangle);
            MouseButtonPressed = false;
            HSelector_Changed(sender);
        }

        private void HSelector_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MouseButtonPressed = true;
        }

        private void HSelector_MouseMove(object sender, MouseEventArgs e)
        {
            if (MouseButtonPressed)
            {
                Point point = e.GetPosition((Canvas)sender);
                ChangeSlider(point.Y);
            }
        }

        private void ChangeSlider(double value)
        {
            if(value >= 0 && value <= HSelector.ActualHeight)
            {
                Canvas.SetLeft(rectangle, (HSelector.ActualWidth - rectangle.Width) / 2);
                Canvas.SetTop(rectangle, value - rectangle.Height / 2);
            }
        }

        private void HSelector_MouseLeave(object sender, MouseEventArgs e)
        {
            if (MouseButtonPressed) 
            {
                MouseButtonPressed = false;
                HSelector_Changed(sender);
            }
        }
        private void HSelector_Changed(Object obj)
        {    
            SLSelect_paint();
            if (obj == HSelector) HSVColorChanged(new HSVCOLOR(this));
        }

        private void SLSelector_Changed(Object obj)
        {
            double v = 1.0 - (Canvas.GetTop(ellipse) + ellipse.Height / 2.0) / HSelector.ActualHeight;
            Color color = v > 0.5 ? Colors.Black : Colors.LightGray;
            ellipse.Stroke = new SolidColorBrush(color);
            if (obj == SLSelector) HSVColorChanged(new HSVCOLOR(this));
        }
        private void HSelect_paint()
        {
            #if DEBUG 
            System.Diagnostics.Debug.WriteLine("HSelect_paint_Start");
            DateTime starttime = DateTime.Now;
            #endif
            ColorConverter CC = new ColorConverter();
            double hsHeight = HSelector.ActualHeight;
            double hsWidth = HSelector.ActualWidth;
            for (int i = 0; i <= (int)Math.Round(hsHeight); i+= linewidth)
            {
                ColorMode.RGB color = CC.HSV2RGB((i / (double)hsHeight) * 360.0, 1, 1);
                SolidColorBrush brush = new SolidColorBrush(Color.FromRgb(
                        (byte)(color.R * 255.0),
                        (byte)(color.G * 255.0),
                        (byte)(color.B * 255.0)
                        ));
                Line line = new Line
                {
                    Stroke = brush,
                    StrokeThickness = linewidth,
                    X1 = 0,
                    X2 = hsWidth,
                    Y1 = hsHeight-i,
                    Y2 = hsHeight-i
                };
                HSelector.Children.Add(line);
            }
            #if DEBUG
            DateTime stoptime = DateTime.Now;
            TimeSpan span = stoptime.Subtract(starttime);
            System.Diagnostics.Debug.WriteLine("HSelect_paint_Stop");
            System.Diagnostics.Debug.WriteLine("Use {0} ms", span.TotalMilliseconds);
            #endif
        }
        private Rectangle[,] pixels;
        private bool isinitialized = false;
        private void SLSelect_paint()
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("SLSelect_paint_Start");
            DateTime starttime = DateTime.Now;
            #endif
            HSVCOLOR hsvcolor = new HSVCOLOR(this);
            ColorConverter CC = new ColorConverter();
            double slWidth = SLSelector.ActualWidth;
            double slHeight = SLSelector.ActualHeight;
            int xMax = (int)Math.Round(slWidth);
            int yMax = (int)Math.Round(slHeight);
            double H = hsvcolor.H != 360 ? hsvcolor.H : 0;
            if (!isinitialized) pixels = new Rectangle[xMax + 1, yMax + 1];
            for (int y = 0; y <= yMax; y += pixelsize)
            {
                for (int x = 0; x <= xMax; x += pixelsize)
                {
                    ColorMode.RGB color = CC.HSV2RGB(H, x / slWidth, (yMax - y) / slHeight);

                    SolidColorBrush brush = new SolidColorBrush(Color.FromRgb(
                    (byte)(color.R * 255.0),
                    (byte)(color.G * 255.0),
                    (byte)(color.B * 255.0)
                    ));
                    Rectangle pixel;
                    if (!isinitialized)
                    {
                        pixel = new Rectangle
                        {
                            Height = pixelsize,
                            Width = pixelsize,
                        };
                        Canvas.SetLeft(pixel, x);
                        Canvas.SetTop(pixel, y);
                        pixels[x, y] = pixel;
                        SLSelector.Children.Add(pixel);
                    }
                    else
                    {
                        pixel = pixels[x, y];
                    }
                    pixel.Fill = brush;
                }
            }
            isinitialized = true; 
            #if DEBUG
            DateTime stoptime = DateTime.Now;
            TimeSpan span = stoptime.Subtract(starttime);
            System.Diagnostics.Debug.WriteLine("SLSelect_paint_Stop");
            System.Diagnostics.Debug.WriteLine("Use {0} ms", span.TotalMilliseconds);
            #endif
        }
        private void SLSelector_MouseMove(object sender, MouseEventArgs e)
        {
            SLSelector.Cursor = new Cursor(e.GetPosition(SLSelector).Y / SLSelector.ActualHeight < 0.5 ? CursorFile : CursorFile2);
        }
       
        private void HSVColorChanged(HSVCOLOR hsvcolor)
        {
            Textbox_TextChanged_Cancel();
            ColorConverter CC = new ColorConverter();
            ColorMode.HSV hsv = new ColorMode.HSV(hsvcolor.H, hsvcolor.S, hsvcolor.V);
            ColorMode.RGB rgb = CC.HSV2RGB(hsv.H != 360 ? hsv.H : 0, hsv.S, hsv.V);
            ColorMode.HSL hsl = CC.RGB2HSL(rgb.R, rgb.G, rgb.B);
            Color color = Color.FromRgb((byte)(rgb.R * 255.0), (byte)(rgb.G * 255.0), (byte)(rgb.B * 255.0));
            ParaH_HSV.Text = ((int)Math.Round(hsv.H)).ToString();
            ParaS_HSV.Text = ((int)Math.Round(hsv.S * 100.0)).ToString();
            ParaV_HSV.Text = ((int)Math.Round(hsv.V * 100.0)).ToString();
            ParaR_RGB.Text = ((int)Math.Round(rgb.R * 255.0)).ToString();
            ParaG_RGB.Text = ((int)Math.Round(rgb.G * 255.0)).ToString();
            ParaB_RGB.Text = ((int)Math.Round(rgb.B * 255.0)).ToString();
            ParaH_HSL.Text = ((int)Math.Round(hsl.H)).ToString();
            ParaS_HSL.Text = ((int)Math.Round(hsl.S * 100.0)).ToString();
            ParaL_HSL.Text = ((int)Math.Round(hsl.L * 100.0)).ToString();
            Para_HEX.Text = new HEXCOLORCONVERTER().COLOR2HEX(color);
            Color_Before.Background = Color_Present.Background;
            Color_Present.Background = new SolidColorBrush(color);
            Textbox_TextChanged_Add();
        }

        private void RGBColorChanged(Color color, object obj)
        {
            Textbox_TextChanged_Cancel();
            ColorConverter CC = new ColorConverter();
            ColorMode.RGB rgb = new ColorMode.RGB(color.R / 255.0, color.G / 255.0, color.B / 255.0);
            ColorMode.HSV hsv = CC.RGB2HSV(rgb.R, rgb.G, rgb.B);
            ColorMode.HSL hsl = CC.RGB2HSL(rgb.R, rgb.G, rgb.B);
            if (obj != ParaH_HSV) ParaH_HSV.Text = ((int)Math.Round(hsv.H)).ToString();
            if (obj != ParaS_HSV) ParaS_HSV.Text = ((int)Math.Round(hsv.S * 100.0)).ToString();
            if (obj != ParaV_HSV) ParaV_HSV.Text = ((int)Math.Round(hsv.V * 100.0)).ToString();
            if (obj != ParaR_RGB) ParaR_RGB.Text = ((int)Math.Round(rgb.R * 255.0)).ToString();
            if (obj != ParaG_RGB) ParaG_RGB.Text = ((int)Math.Round(rgb.G * 255.0)).ToString();
            if (obj != ParaB_RGB) ParaB_RGB.Text = ((int)Math.Round(rgb.B * 255.0)).ToString();
            if (obj != ParaH_HSL) ParaH_HSL.Text = ((int)Math.Round(hsl.H)).ToString();
            if (obj != ParaS_HSL) ParaS_HSL.Text = ((int)Math.Round(hsl.S * 100.0)).ToString();
            if (obj != ParaL_HSL) ParaL_HSL.Text = ((int)Math.Round(hsl.L * 100.0)).ToString();
            if (obj != Para_HEX) Para_HEX.Text = new HEXCOLORCONVERTER().COLOR2HEX(color);
            Color_Before.Background = Color_Present.Background;
            Color_Present.Background = new SolidColorBrush(color);
            HSVCOLOR hsvcolor = new HSVCOLOR(this, hsv.H, hsv.S, hsv.V);
            Canvas.SetLeft(rectangle, hsvcolor.HSelectorX);
            Canvas.SetTop(rectangle, hsvcolor.HSelectorY);
            Canvas.SetLeft(ellipse, hsvcolor.SLSelectorX);
            Canvas.SetTop(ellipse, hsvcolor.SLSelectorY);
            HSelector_Changed(obj);
            SLSelector_Changed(obj);
            Textbox_TextChanged_Add();
        }

        private void Color_Before_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SolidColorBrush brush = Color_Before.Background as SolidColorBrush;
            RGBColorChanged(brush.Color, sender);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.straw.Focus();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(Properties.Resources.csscolors);
            XmlElement root = doc.DocumentElement;
            if (!Regex.IsMatch(Para_HEX.Text, "'"))
            {
                string textboxText = Para_HEX.Text;
                if (Para_HEX.Text.Length > 0)
                {
                    if (Para_HEX.Text[0] >= 'a' && Para_HEX.Text[0] <= 'z')
                    {
                        textboxText = Para_HEX.Text.Substring(0, 1).ToUpper() + Para_HEX.Text.Substring(1);
                    }
                }
                XmlNodeList nodelist = root.SelectNodes("color[@name='" + textboxText + "']");
                if (nodelist.Count > 0)
                {
                    string value = nodelist[0].Attributes["value"].Value;
                    Para_HEX.TextChanged -= new TextChangedEventHandler(Para_HEX_TextChanged);
                    Para_HEX.TextChanged -= new TextChangedEventHandler(Para_TextChanged);
                    Para_HEX.Text = value;
                    Para_HEX.TextChanged += new TextChangedEventHandler(Para_TextChanged);
                    Para_HEX.TextChanged += new TextChangedEventHandler(Para_HEX_TextChanged);
                }
            }
        }

        private void Para_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                //Textbox_TextChanged_Cancel();
                TextBox textbox = (TextBox)sender;
                //Regex.IsMatch(textbox.Text, "^[0-9]+$");
                bool isRGB = sender == ParaR_RGB || sender == ParaG_RGB || sender == ParaB_RGB;
                bool isHSV = sender == ParaH_HSV || sender == ParaS_HSV || sender == ParaV_HSV;
                bool isHSL = sender == ParaH_HSL || sender == ParaS_HSL || sender == ParaL_HSL;
                bool isHEX = sender == Para_HEX;
                if (isRGB)
                {
                    if (Regex.IsMatch(textbox.Text.Trim(), "^[0-9]*$"))
                    {
                        int R = ParaR_RGB.Text != "" ? Convert.ToInt32(ParaR_RGB.Text) : 0;
                        int G = ParaG_RGB.Text != "" ? Convert.ToInt32(ParaG_RGB.Text) : 0;
                        int B = ParaB_RGB.Text != "" ? Convert.ToInt32(ParaB_RGB.Text) : 0;
                        if (
                               R >= 0 && R <= 255
                            && G >= 0 && G <= 255
                            && B >= 0 && B <= 255
                            )
                        {
                            RGBColorChanged(Color.FromRgb((byte)R, (byte)G, (byte)B), sender);
                        }
                    }
                    else
                    {
                        throw new FormatException("输入字符串的格式不正确。");
                    }
                }
                else if (isHSV)
                {
                    if (Regex.IsMatch(textbox.Text, "^[0-9]*$"))
                    {
                        int H = ParaH_HSV.Text != "" ? Convert.ToInt32(ParaH_HSV.Text) : 0;
                        int S = ParaS_HSV.Text != "" ? Convert.ToInt32(ParaS_HSV.Text) : 0;
                        int V = ParaV_HSV.Text != "" ? Convert.ToInt32(ParaV_HSV.Text) : 0;
                        if (
                               H >= 0 && H <= 360
                            && S >= 0 && S <= 100
                            && V >= 0 && V <= 100
                            )
                        {
                            Textbox_TextChanged_Cancel();
                            ColorConverter CC = new ColorConverter();
                            HSVCOLOR hsvcolor = new HSVCOLOR(this, H, S / 100.0, V / 100.0);
                            ColorMode.RGB rgb = CC.HSV2RGB(H != 360 ? H : 0, S / 100.0, V / 100.0);
                            ColorMode.HSL hsl = CC.RGB2HSL(rgb.R, rgb.G, rgb.B);
                            Color color = Color.FromRgb((byte)(rgb.R * 255.0), (byte)(rgb.G * 255.0), (byte)(rgb.B * 255.0));
                            ParaR_RGB.Text = ((int)Math.Round(rgb.R * 255.0)).ToString();
                            ParaG_RGB.Text = ((int)Math.Round(rgb.G * 255.0)).ToString();
                            ParaB_RGB.Text = ((int)Math.Round(rgb.B * 255.0)).ToString();
                            ParaH_HSL.Text = ((int)Math.Round(hsl.H)).ToString();
                            ParaS_HSL.Text = ((int)Math.Round(hsl.S * 100.0)).ToString();
                            ParaL_HSL.Text = ((int)Math.Round(hsl.L * 100.0)).ToString();
                            Para_HEX.Text = new HEXCOLORCONVERTER().COLOR2HEX(color);
                            Canvas.SetLeft(rectangle, hsvcolor.HSelectorX);
                            Canvas.SetTop(rectangle, hsvcolor.HSelectorY);
                            Canvas.SetLeft(ellipse, hsvcolor.SLSelectorX);
                            Canvas.SetTop(ellipse, hsvcolor.SLSelectorY);
                            HSelector_Changed(textbox);
                            SLSelector_Changed(textbox);
                            Color_Before.Background = Color_Present.Background;
                            Color_Present.Background = new SolidColorBrush(color);
                            Textbox_TextChanged_Add();
                        }
                    }
                    else
                    {
                        throw new FormatException("输入字符串的格式不正确。");
                    }
                }
                else if (isHSL)
                {
                    if (Regex.IsMatch(textbox.Text, "^[0-9]*$"))
                    {
                        int H = ParaH_HSL.Text != "" ? Convert.ToInt32(ParaH_HSL.Text) : 0;
                        int S = ParaS_HSL.Text != "" ? Convert.ToInt32(ParaS_HSL.Text) : 0;
                        int L = ParaL_HSL.Text != "" ? Convert.ToInt32(ParaL_HSL.Text) : 0;
                        if (
                               H >= 0 && H <= 360
                            && S >= 0 && S <= 100
                            && L >= 0 && L <= 100
                            )
                        {
                            Textbox_TextChanged_Cancel();
                            ColorConverter CC = new ColorConverter();
                            ColorMode.RGB rgb = CC.HSL2RGB(H != 360 ? H : 0, S / 100.0, L / 100.0);
                            ColorMode.HSV hsv = CC.RGB2HSV(rgb.R, rgb.G, rgb.B);
                            Color color = Color.FromRgb((byte)(rgb.R * 255.0), (byte)(rgb.G * 255.0), (byte)(rgb.B * 255.0));
                            ParaR_RGB.Text = ((int)Math.Round(rgb.R * 255.0)).ToString();
                            ParaG_RGB.Text = ((int)Math.Round(rgb.G * 255.0)).ToString();
                            ParaB_RGB.Text = ((int)Math.Round(rgb.B * 255.0)).ToString();
                            ParaH_HSV.Text = ((int)Math.Round(hsv.H)).ToString();
                            ParaS_HSV.Text = ((int)Math.Round(hsv.S * 100.0)).ToString();
                            ParaV_HSV.Text = ((int)Math.Round(hsv.V * 100.0)).ToString();
                            Para_HEX.Text = new HEXCOLORCONVERTER().COLOR2HEX(color);
                            HSVCOLOR hsvcolor = new HSVCOLOR(this, hsv.H, hsv.S, hsv.V);
                            Canvas.SetLeft(rectangle, hsvcolor.HSelectorX);
                            Canvas.SetTop(rectangle, hsvcolor.HSelectorY);
                            Canvas.SetLeft(ellipse, hsvcolor.SLSelectorX);
                            Canvas.SetTop(ellipse, hsvcolor.SLSelectorY);
                            HSelector_Changed(textbox);
                            SLSelector_Changed(textbox);
                            Color_Before.Background = Color_Present.Background;
                            Color_Present.Background = new SolidColorBrush(color);
                            Textbox_TextChanged_Add();
                        }
                    }
                    else
                    {
                        throw new FormatException("输入字符串的格式不正确。");
                    }
                }
                else if (isHEX)
                {
                    if (Regex.IsMatch(textbox.Text, "^#[0-9A-Fa-f]+$"))
                    {
                        Color color = new HEXCOLORCONVERTER().HEX2COLOR(textbox.Text.PadRight(7, '0'));
                        if (color != ((SolidColorBrush)Color_Present.Background).Color)
                        {
                            RGBColorChanged(Color.FromRgb((byte)color.R, (byte)color.G, (byte)color.B), sender);
                        }
                    }
                    else
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(Properties.Resources.csscolors);
                        XmlElement root = doc.DocumentElement;
                        if (!Regex.IsMatch(textbox.Text, "'"))
                        {
                            string textboxText = textbox.Text;
                            if (textbox.Text.Length > 0)
                            {
                                if (textbox.Text[0] >= 'a' && textbox.Text[0] <= 'z')
                                {
                                    textboxText = textbox.Text.Substring(0, 1).ToUpper() + textbox.Text.Substring(1);
                                }
                            }
                            XmlNodeList nodelist = root.SelectNodes("color[@name='" + textboxText + "']");
                            if (nodelist.Count > 0)
                            {
                                string value = nodelist[0].Attributes["value"].Value;
                                Color color = new HEXCOLORCONVERTER().HEX2COLOR(value);
                                if (color != ((SolidColorBrush)Color_Present.Background).Color)
                                {
                                    RGBColorChanged(Color.FromRgb((byte)color.R, (byte)color.G, (byte)color.B), sender);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                /*
                Thread thread = new Thread(new ThreadStart(() => { 
                    MessageBox.Show(err.Message, "错误"); 
                }));
                thread.Start();
                thread.Join();
                */
                MessageBox.Show(err.Message, "错误");
            }
        }
        
        private void Textbox_TextChanged_Cancel()
        {
            foreach(TextBox textbox in TextBoxList)
            {
                textbox.TextChanged -= new TextChangedEventHandler(Para_TextChanged);
            }
        }
        private void Textbox_TextChanged_Add()
        {
            foreach (TextBox textbox in TextBoxList)
            {
                textbox.TextChanged += new TextChangedEventHandler(Para_TextChanged);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            HSVCOLOR hsvcolor = new HSVCOLOR(this);
            StreamReader sr = new StreamReader(SettingFile);
            JsonTextReader jtr = new JsonTextReader(sr);
            JObject obj = (JObject)JToken.ReadFrom(jtr);
            JToken jtwindow = obj["window"];
            JToken jtcolor = obj["color"];
            jtwindow["top"] = this.Top;
            jtwindow["left"] = this.Left;
            jtcolor["H"] = hsvcolor.H;
            jtcolor["S"] = hsvcolor.S;
            jtcolor["V"] = hsvcolor.V;
            sr.Close();
            StreamWriter sw = new StreamWriter(SettingFile);
            sw.Write(obj.ToString());
            sw.Flush();
            sw.Close();
        }

        private void Para_HEX_TextChanged(object sender, TextChangedEventArgs e)
        {
            Thread thread = new Thread(new ThreadStart(() => {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(Properties.Resources.csscolors);
                XmlElement root = doc.DocumentElement;
                string textboxText = null;
                Dispatcher.Invoke(new Action(() => { textboxText = Para_HEX.Text; }));
                if (!Regex.IsMatch(textboxText, "'"))
                {
                    if(textboxText.Length > 0)
                    {
                        if (textboxText[0] >= 'a' && textboxText[0] <= 'z')
                        {
                            textboxText = textboxText.Substring(0, 1).ToUpper() + textboxText.Substring(1);
                        }
                    }
                    XmlNodeList nodelist =
                    root.SelectNodes("color[@name='" + textboxText + "']" +
                    "|color[@value='" + textboxText + "']");
                    Dispatcher.Invoke(new Action(() =>
                    {
                        ColorName.Content = nodelist.Count > 0 ?
                        "颜色名：" + nodelist[0].Attributes["name"].Value : "";
                    }));
                }
                else
                {
                    Dispatcher.Invoke(new Action(() => { ColorName.Content = ""; }));       
                }
            }));
            thread.Start();
        }

        private class HEXCOLORCONVERTER
        {
            private string HEXNUM = "0123456789ABCDEF";
            public string COLOR2HEX(Color color)
            {
                return "#"
                    + NUMTOHEX((int)color.R)
                    + NUMTOHEX((int)color.G)
                    + NUMTOHEX((int)color.B);
            }
            public Color HEX2COLOR(string hex)
            {
                hex = hex.Replace("#", "");
                int r = HEXTONUM(hex.Substring(0, 2).ToUpper());
                int g = HEXTONUM(hex.Substring(2, 2).ToUpper());
                int b = HEXTONUM(hex.Substring(4, 2).ToUpper());
                return Color.FromRgb((byte)r, (byte)g, (byte)b);
            }
            private string NUMTOHEX(int number)
            {
                string HEX = null;
                if (number == 0) HEX = "00";
                while (number > 0)
                {
                    HEX = HEXNUM[number % 16] + HEX;
                    number /= 16;
                }
                if (HEX.Length <= 1) HEX = "0" + HEX;
                return HEX;
            }
            private int HEXTONUM(string hex)
            {
                int index, number = 0;
                for(int i= 0; i < hex.Length; i++)
                {
                    index = hex.Length - i - 1;
                    for (int j = 0; j < HEXNUM.Length; j++)
                    {
                        if(hex[index] == HEXNUM[j])
                        {
                            number += j * (int)Math.Pow(16, i);
                            break;
                        }
                    }               
                }
                return number;
            }
        }
    }
}
