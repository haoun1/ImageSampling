using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImgSampleApplication
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        MemoryMappedFile m_MMF;
        MemoryMappedViewStream m_MMVS;
        long m_Adress;
        int fGB = 5;
        BitmapSource m_bitmapSource;
     
        public BitmapSource p_bitmapSource
        {
            get => m_bitmapSource;
            set
            {
                m_bitmapSource = value;
                OnPropertyChanged("m_bitmapSource");
            }
        }

        public MainWindowViewModel()
        {
            m_MMF = MemoryMappedFile.CreateOrOpen("Memory", fGB * 1024 * 1024 * 1024);

        }

        public RelayCommand ImageLoadCommand
        {
            get => new RelayCommand(ImageLoad);
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
        private void ImageLoad()
        {

            FileStream fs;
            BinaryReader br;
            uint bfOffbits = 0;
            int width = 0;
            int height = 0;
            int nByte = 0;
            byte[] abuf;
            IntPtr destPtr;
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
                    unsafe
                    {
                        byte* p = null;
                        m_MMF.CreateViewAccessor().SafeMemoryMappedViewHandle.AcquirePointer(ref p);
                        destPtr = new IntPtr(p);
                    }
  
                    if (!ReadBitmapFileHeader(br,ref bfOffbits)) return;
                    if (!ReadBitmapInfoHeader(br, ref width, ref height, ref nByte)) return;
                    if (nByte > 1) return;

                    fileRowSize = (width * nByte + 3) & ~3; // 파일 내 하나의 열당 너비 사이즈(4의 배수)
                    Rectangle rect = new Rectangle(0, 0, width, height);
                    abuf = new byte[rect.Width * nByte];

                    // 픽셀 데이터 존재하는 부분으로 Seek
                    fs.Seek(bfOffbits, SeekOrigin.Begin);
                    
                    for(int i = rect.Bottom -1; i>=rect.Top; i--)
                    {
                        Array.Clear(abuf, 0, rect.Width * nByte);
                        fs.Seek(rect.Left * nByte, SeekOrigin.Current); // Offset이 없으면 주석처리가능
                        fs.Read(abuf, 0, rect.Width * nByte);
               
                        Marshal.Copy(abuf, 0, destPtr, rect.Width * nByte);
                        fs.Seek(fileRowSize - rect.Right * nByte, SeekOrigin.Current); // Offset이 없으면 주석처리가능
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

        private bool ReadBitmapInfoHeader(BinaryReader br, ref int width, ref int height, ref int nByte)
        {
            if (br == null) return false;

            uint biSize;

            biSize = br.ReadUInt32();     // biSize
            width = br.ReadInt32();       // biWidth
            height = br.ReadInt32();      // biHeight
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
