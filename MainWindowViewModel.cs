using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImgSampleApplication
{
    enum ColorMode
    {
        Mono,
        Color
    }

    class MainWindowViewModel : INotifyPropertyChanged
    {
        MemoryMappedFile m_MMF;
        MemoryMappedViewStream m_MMVS;
        long m_Adress;
        double fGB = 5;
        double GB = 1024 * 1024 * 1024;
        int MapSizeX;
        int MapSizeY;
        int CanvasBit_Width = 800;
        ColorMode m_color;
        public int p_CanvasWidth
        {
            get => CanvasBit_Width;
            set
            {
                CanvasBit_Width = value;
                OnPropertyChanged();
            }
        }
        int CanvasBit_Height = 450;
        public int p_CanvasHeight
        {
            get => CanvasBit_Height;
            set
            {
                CanvasBit_Height = value;
                OnPropertyChanged();
                ImageOpen();
            }
        }
        uint bfOffbits = 0;
        int Bit_Width = 0;
        int Bit_Height = 0;
        int nByte = 0;
        BitmapSource m_bitmapSource;
        IntPtr RPtr, GPtr, BPtr;

        int m_mouseX;
        public int p_mouseX
        {
            get => m_mouseX;
            set
            {
                m_mouseX = value;
                OnPropertyChanged();
            }
        }

        int m_mouseY;
        public int p_mouseY
        {
            get => m_mouseY;
            set
            {
                unsafe
                {
                    byte* arrByte = (byte*)RPtr.ToPointer();
                    long idx = p_mouseMemX + ((long)p_mouseMemY * MapSizeX);
                    byte b1 = arrByte[idx];
                    p_pixelData = BitConverter.ToUInt16(new byte[2] { b1, 0 }, 0);
                    int SamplingRate_y = MapSizeY < p_CanvasHeight ? 1 : MapSizeY / p_CanvasHeight;
                    int SamplingRate_x = MapSizeX < p_CanvasWidth ? 1 : MapSizeX / p_CanvasWidth;
                    p_mouseMemX = p_mouseX * SamplingRate_x;
                    p_mouseMemY = p_mouseY * SamplingRate_y;
                }
                m_mouseY = value;
                OnPropertyChanged();
            }
        }

        int m_mouseMemX;
        public int p_mouseMemX
        {
            get => m_mouseMemX;
            set
            {
                m_mouseMemX = value;
                OnPropertyChanged();
            }
        }

        int m_mouseMemY;
        public int p_mouseMemY
        {
            get => m_mouseMemY;
            set
            {
                m_mouseMemY = value;
                OnPropertyChanged();
            }
        }

        int m_pixelData;
        public int p_pixelData
        {
            get => m_pixelData;
            set
            {
                m_pixelData = value;
                OnPropertyChanged();
            }
        }

        public BitmapSource p_bitmapSource
        {
            get => m_bitmapSource;
            set
            {
                m_bitmapSource = value;
                OnPropertyChanged();
            }
        }

        public MainWindowViewModel()
        {
            long nPool = (long)Math.Ceiling(fGB * GB);
            m_MMF = MemoryMappedFile.CreateOrOpen("Memory", nPool);
            MapSizeX = 40000;
            MapSizeY = 40000;
            unsafe 
            { 
                byte* p = null;
                m_MMF.CreateViewAccessor().SafeMemoryMappedViewHandle.AcquirePointer(ref p);
                RPtr = new IntPtr(p);
            }
            ImageOpen();
        }

        public RelayCommand ImageLoadCommand
        {
            get => new RelayCommand(ImageLoad);
        }

        public RelayCommand ImageClearCommand
        {
            get => new RelayCommand(ImageClear);
        }


        public RelayCommand loadedCommand
        {
            get => new RelayCommand(() => 
            { 
            
            });
        }
        public RelayCommand UnloadedCommand
        {
            get => new RelayCommand(() =>
            {
                m_MMF.Dispose();
            });
        }

        /// <summary>
        /// 이미지 샘플링을 위한 비트맵소스 읽어오는 함수
        /// </summary>
        /// <list type="table">
        /// <listheader>
        ///    <term>22-09-16</term>
        ///    <term>이하운</term>
        ///    <term>다이얼로그에서 읽어옵니다</term>
        ///    <term>비고</term>
        ///    </listheader>
        /// <item>
        ///    <term>2022-09-16</term>
        ///    <term>이하운</term>
        ///    <term>생성</term>
        ///    <term>-</term>
        /// </item>
        /// </list>
        /// <param name="args">없음</param>
        /// <returns> 없음 </returns>
        private unsafe void ImageLoad()
        {
            FileStream fs;
            BinaryReader br;
            byte[] abuf;
            int fileRowSize;
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Image Files|*.bmp";
            dlg.InitialDirectory = @"D:\Images";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                fs = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read, FileShare.Read, 32768, true);
                br = new BinaryReader(fs);
                try
                {
  
                    if (!ReadBitmapFileHeader(br,ref bfOffbits)) return;
                    if (!ReadBitmapInfoHeader(br, ref Bit_Width, ref Bit_Height, ref nByte)) return;
                    GPtr = (IntPtr)((long)RPtr + (long)(Bit_Width * Bit_Height));
                    BPtr = (IntPtr)((long)GPtr + (long)(Bit_Width * Bit_Height));
                    MapSizeX = Bit_Width;
                    MapSizeY = Bit_Height;
                    if (nByte > 1) m_color = ColorMode.Color;
                    else m_color = ColorMode.Mono;

                    if (m_color == ColorMode.Mono)
                    {
                        fileRowSize = (Bit_Width * nByte + 3) & ~3; // 파일 내 하나의 열당 너비 사이즈(4의 배수)
                        Rectangle rect = new Rectangle(0, 0, Bit_Width, Bit_Height);
                        abuf = new byte[rect.Width * nByte];

                        // 픽셀 데이터 존재하는 부분으로 Seek
                        fs.Seek(bfOffbits, SeekOrigin.Begin);

                        //for(int i = rect.Bottom -1; i>=rect.Top; i--)
                        for (int i = rect.Bottom - 1; i >= rect.Top; i--)
                        {
                            Array.Clear(abuf, 0, rect.Width * nByte);
                            fs.Seek(rect.Left * nByte, SeekOrigin.Current); // Offset이 없으면 주석처리가능
                            fs.Read(abuf, 0, rect.Width * nByte);

                            IntPtr ptr = new IntPtr(RPtr.ToInt64() + ((long)i) * MapSizeX * nByte);
                            Marshal.Copy(abuf, 0, ptr, abuf.Length);
                            fs.Seek(fileRowSize - rect.Right * nByte, SeekOrigin.Current); // Offset이 없으면 주석처리가능
                        }
                        System.Windows.Forms.MessageBox.Show("Image Load Done");
                    }
                    else if(m_color == ColorMode.Color)
                    {
                        fileRowSize = Bit_Width * nByte;
                        abuf = new byte[Bit_Width * nByte];
                        Rectangle rect = new Rectangle(0, 0, Bit_Width, Bit_Height);
                        fs.Seek(bfOffbits, SeekOrigin.Begin);
                        for (int yy = rect.Bottom - 1 ; yy >= rect.Top; yy--)
                        {
                            Array.Clear(abuf, 0, Bit_Width * nByte);
                            //fs.Seek(Bit_Width * nByte, SeekOrigin.Current);
                            fs.Read(abuf, 0, Bit_Width * nByte);
                            Parallel.For(0, Bit_Width, new ParallelOptions { MaxDegreeOfParallelism = 12 },(xx) => 
                            {
                                int idx = yy * MapSizeX + xx;
                                ((byte*)RPtr)[idx] = abuf[xx * nByte];
                                ((byte*)GPtr)[idx] = abuf[xx * nByte + 1];
                                ((byte*)BPtr)[idx] = abuf[xx * nByte + 2];
                            });
                        }
                        System.Windows.Forms.MessageBox.Show("Image Load Done");
                    }

                }
                catch(Exception e)
                {
                    return;
                }
                finally
                {
                    fs.Close();
                    br.Close();
                }

            }
        }

        private unsafe void ImageOpen()
        {
            try
            {
                switch (m_color)
                {
                    case ColorMode.Mono:
                        {
                            if (RPtr != IntPtr.Zero)
                            {
                                Image<Gray, byte> view = new Image<Gray, byte>(p_CanvasWidth, p_CanvasHeight);
                                int SamplingRate_y = MapSizeY < p_CanvasHeight ? 1 : MapSizeY / p_CanvasHeight;
                                int SamplingRate_x = MapSizeX < p_CanvasWidth ? 1 : MapSizeX / p_CanvasWidth;
                                byte[,,] viewptr = view.Data;
                                Parallel.For(0, p_CanvasHeight, (yy) =>
                                {
                                    long pix_y = yy * SamplingRate_y;
                                    for (int xx = 0; xx < p_CanvasWidth; xx++)
                                    {
                                        long pix_x = xx * SamplingRate_x;
                                        byte* arrByte = (byte*)RPtr;
                                        long idx = pix_x + (pix_y * MapSizeX);
                                        byte pixel = arrByte[idx];
                                        viewptr[yy, xx, 0] = pixel;
                                    }
                                });
                                p_bitmapSource = ToBitmapSource(view);
                            }
                            else
                            {
                                System.Windows.Forms.MessageBox.Show("INTPTR Zero");
                            }
                        }
                        break;
                    case ColorMode.Color:
                        if(RPtr != IntPtr.Zero)
                        {
                            Image<Rgb, byte> view = new Image<Rgb, byte>(p_CanvasWidth,p_CanvasHeight);
                            int SamplingRate_y = MapSizeY < p_CanvasHeight ? 1 : MapSizeY / p_CanvasHeight;
                            int SamplingRate_x = MapSizeX < p_CanvasWidth ? 1 : MapSizeX / p_CanvasWidth;
                            byte[,,] viewPtr = view.Data;
                            byte* RByte = (byte*)RPtr.ToPointer();
                            byte* GByte = (byte*)GPtr.ToPointer();
                            byte* BByte = (byte*)BPtr.ToPointer();
                            Parallel.For(0, MapSizeY < p_CanvasHeight ? MapSizeY : p_CanvasHeight , (yy) => 
                            {
                                long pix_y =  yy * SamplingRate_y;
                                for(int xx = 0; xx < (MapSizeX < p_CanvasWidth ? MapSizeX:p_CanvasWidth); xx++)
                                {
                                    long pix_x = xx * SamplingRate_x;
                                    long idx = pix_x + (pix_y * MapSizeX);
                                    viewPtr[yy, xx, 0] = RByte[idx];
                                    viewPtr[yy, xx, 1] = GByte[idx];
                                    viewPtr[yy, xx, 2] = BByte[idx];
                                }
                            });
                            p_bitmapSource = ToBitmapSource(view);
                        }
                        break;
                }
            }
            catch(Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }
        }

        private unsafe void ImageClear()
        {
            byte[] abuf = new byte[MapSizeX * nByte];
            for (int i = 0; i <= MapSizeY; i++)
            {
                IntPtr ptr = new IntPtr(RPtr.ToInt64() + ((long)i) * MapSizeX * nByte);
                Marshal.Copy(abuf, 0, ptr, abuf.Length);
            }
            System.Windows.Forms.MessageBox.Show("Image Clear Done");
        }

        private bool ReadBitmapFileHeader(BinaryReader br, ref uint bfOffbits)
        {
            if (br == null) return false;

            ushort bfType;
            uint bfSize;

            bfType = br.ReadUInt16(); // isBitmap or Not
            bfSize = br.ReadUInt32(); // FileSize
            br.ReadUInt16(); //Reserved1 (NotUsed)
            br.ReadUInt16(); //Reserved2 (NotUsed)
            bfOffbits = br.ReadUInt32(); // biOffset

            if (bfType != 0x4d42) return false;

            return true;

        }

        private BitmapSource ToBitmapSource(Image<Gray, byte> img)
        {
            using(Bitmap source = img.ToBitmap())
            {
                var bitmapData = source.LockBits(
                    new Rectangle(0, 0, source.Width, source.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    source.PixelFormat);
                BitmapSource result = BitmapSource.Create(source.Width, source.Height, 96, 96, 
                    PixelFormats.Gray8, null, bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
                source.UnlockBits(bitmapData);
                return result;
            }
        }

        private BitmapSource ToBitmapSource(Image<Rgb, byte> img)
        {
            using (Bitmap source = img.ToBitmap())
            {
                var bitmapData = source.LockBits(
                    new Rectangle(0, 0, source.Width, source.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    source.PixelFormat);
                BitmapSource result = BitmapSource.Create(source.Width, source.Height, 96, 96,
                    PixelFormats.Rgb24, null, bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
                source.UnlockBits(bitmapData);
                return result;
            }
        }

        private bool ReadBitmapInfoHeader(BinaryReader br, ref int Bit_Width, ref int Bit_Height, ref int nByte)
        {
            if (br == null) return false;

            uint biSize;

            biSize = br.ReadUInt32();     // biSize
            Bit_Width = br.ReadInt32();       // biBit_Width
            Bit_Height = br.ReadInt32();      // biBit_Height
            br.ReadUInt16();              // biPlanes
            nByte = br.ReadUInt16() / 8;  // biBitcount
            br.ReadUInt32();              // biCompression
            br.ReadUInt32();              // biSizeImage
            br.ReadInt32();               // biXPelsPerMeter
            br.ReadInt32();               // biYPelsPerMeter
            br.ReadUInt32();              // biClrUsed
            br.ReadUInt32();              // biClrImportant

            return true;
        }

        public void MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var pt = e.GetPosition(sender as IInputElement);
            p_mouseX = (int)pt.X;
            p_mouseY = (int)pt.Y;
        }
       
        public void MouseWheel(object sender, MouseWheelEventArgs e)
        {
            ImageOpen();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class RelayCommand : ICommand
    {

        #region Declarations

        readonly Func<Boolean> _canExecute;
        readonly Action _execute;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand&lt;T&gt;"/> class and the command can always be executed.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        public RelayCommand(Action execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        public RelayCommand(Action execute, Func<Boolean> canExecute)
        {

            if (execute == null)
                throw new ArgumentNullException("execute");
            _execute = execute;
            _canExecute = canExecute;
        }

        #endregion

        #region ICommand Members

        public event EventHandler CanExecuteChanged
        {
            add
            {

                if (_canExecute != null)
                    CommandManager.RequerySuggested += value;
            }
            remove
            {

                if (_canExecute != null)
                    CommandManager.RequerySuggested -= value;
            }
        }

        [DebuggerStepThrough]
        public Boolean CanExecute(Object parameter)
        {
            return _canExecute == null ? true : _canExecute();
        }

        public void Execute(Object parameter)
        {
            try
            {
                _execute();
            }
            catch (Exception)
            {
            }
        }

        #endregion
    }
}
