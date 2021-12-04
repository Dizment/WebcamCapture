using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Emgu;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.CvEnum;

using DirectShowLib;
using System.Globalization;
using System.Runtime.InteropServices;

namespace WebCamVideoCapture
{
    public partial class Form1 : Form
    {
        private static CascadeClassifier classifier = new CascadeClassifier("haarcascade_eye.xml");
        //захватывает видео
        private VideoCapture capture = null;
        //массив доступных камер
        private DsDevice[] webCams = null;

        private double alpha = 1.0;
        private int beta = 0;

        public Form1()
        {
            InitializeComponent();
        }

        //загрузка формы
        private void Form1_Load(object sender, EventArgs e)
        {
            webCams = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            for (int i = 0; i < webCams.Length; i++)
                toolStripComboBox1.Items.Add(webCams[i].Name);

            if (webCams.Length == 1)
                toolStripComboBox1.SelectedIndex = 0;
        }

        //смотреть
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (this.webCams.Length == 0)
            {
                MessageBox.Show("Нет доступных камер!", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (toolStripComboBox1.SelectedIndex == -1)
            {
                MessageBox.Show("Необходимо выбрать камеру.", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                if (capture == null)
                {
                    capture = new VideoCapture(toolStripComboBox1.SelectedIndex);
                    capture.ImageGrabbed += Capture_ImageGrabbed;
                }

                capture.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Capture_ImageGrabbed(object sender, EventArgs e)
        {
            try
            {
                try
                {
                    alpha = double.Parse(toolStripTextBox1.Text, CultureInfo.InvariantCulture);
                    beta = int.Parse(toolStripTextBox2.Text, CultureInfo.InvariantCulture);
                }
                catch
                {
                    return;
                }

                Mat m = new Mat();
                capture.Retrieve(m);
                Mat newImage = Mat.Zeros(m.Size.Height, m.Size.Width, m.Depth, m.NumberOfChannels);

                Mat outImage = Mat.Zeros(m.Size.Height, m.Size.Width, m.Depth, m.NumberOfChannels);

                byte[] color = new byte[9];

                for (int j = 0; j < m.Rows; j++)
                {
                    for (int i = 0; i < m.Cols - 1; i++)
                    {
                        Marshal.Copy(m.DataPointer + (j * m.Cols + i) * 3, color, 0, 6);
                        Marshal.Copy(m.DataPointer + ((j + 1) * m.Cols + i) * 3, color, 6, 3);

                        int a = (Math.Abs(color[0] - color[3]) + Math.Abs(color[1] - color[4]) + Math.Abs(color[2] - color[5]));
                        int b = (Math.Abs(color[0] - color[6]) + Math.Abs(color[1] - color[7]) + Math.Abs(color[2] - color[8]));
                        
                        a = a + b;
                        
                        if (a < 50)
                        {
                            a = 0;
                        }

                        color[0] = (byte)Math.Min(255, a * 10);
                        color[1] = 0;
                        color[2] = 0;

                        Marshal.Copy(color, 0, outImage.DataPointer + (j * m.Cols + i) * 3, 3);
                    }
                }

                m.ConvertTo(newImage, m.Depth, alpha, beta);
                Bitmap bitmap = newImage.ToImage<Bgr, byte>().Flip(FlipType.Horizontal).Bitmap;
                Image<Bgr, byte> grayImage = new Image<Bgr, byte>(bitmap);
                Rectangle[] eyes = classifier.DetectMultiScale(grayImage, 1.1, 5, new Size(30, 30), new Size(200, 200));
                
                //Image<Bgr, byte> inputImage = m.ToImage<Bgr, byte>().Flip(FlipType.Horizontal);
                //Image<Gray, byte> outputImage = inputImage.Convert<Gray, byte>().ThresholdBinary(new Gray(100), new Gray(255)); // thresholdBinary используем для четкого поиска контуров, чтобы избежать шумы
                //VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                //CvInvoke.FindContours(inputImage, contours, m, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                //CvInvoke.DrawContours(inputImage, contours, -1, new MCvScalar(0, 0, 255), 3);

                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    using (Pen pen = new Pen(Color.Yellow, 3))
                    {
                        foreach (var eye in eyes)
                        {
                            graphics.DrawRectangle(pen, eye);
                        }
                    }
                }

                pictureBox1.Image = outImage.Bitmap;
                pictureBox2.Image = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            try
            {
                if (!(capture is null))
                {
                    capture.Pause();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            try
            {
                if (!(capture is null))
                {
                    capture.Pause();
                    capture.Dispose();
                    capture = null;
                    pictureBox1.Image.Dispose();
                    pictureBox1.Image = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
