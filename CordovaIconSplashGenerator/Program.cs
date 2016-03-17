using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace CordovaIconSplashGenerator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            TraverseTree(Properties.Settings.Default.rootPath);
        }

        private static void TraverseTree(string root)
        {
            // Data structure to hold names of subfolders to be
            // examined for files.
            Stack<string> dirs = new Stack<string>(20);

            if (!Directory.Exists(root))
            {
                throw new ArgumentException(root);
            }
            dirs.Push(root);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                string[] subDirs;
                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }
                // An UnauthorizedAccessException exception will be thrown if we do not have
                // discovery permission on a folder or file. It may or may not be acceptable
                // to ignore the exception and continue enumerating the remaining files and
                // folders. It is also possible (but unlikely) that a DirectoryNotFound exception
                // will be raised. This will happen if currentDir has been deleted by
                // another application or thread after our call to Directory.Exists. The
                // choice of which exceptions to catch depends entirely on the specific task
                // you are intending to perform and also on how much you know with certainty
                // about the systems on which this code will run.
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                string[] files = null;
                try
                {
                    files = Directory.GetFiles(currentDir);
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                // Perform the required action on each file here.
                // Modify this block to perform your required task.
                Parallel.ForEach(files, file =>
                {
                    try
                    {
                        // Perform whatever action is required in your scenario.
                        FileInfo fi = new FileInfo(file);
                        //Console.WriteLine("{0}: {1}, {2}", fi.Name, fi.Length, fi.CreationTime);
                        CheckFile(fi);
                    }
                    catch (FileNotFoundException e)
                    {
                        // If file was deleted by a separate application
                        //  or thread since the call to TraverseTree()
                        // then just continue.
                        Console.WriteLine(e.Message);
                    }
                });
                //seriell
                //foreach (string file in files)
                //{
                //    try
                //    {
                //        // Perform whatever action is required in your scenario.
                //        System.IO.FileInfo fi = new System.IO.FileInfo(file);
                //        //Console.WriteLine("{0}: {1}, {2}", fi.Name, fi.Length, fi.CreationTime);
                //        CheckFile(fi);
                //    }
                //    catch (System.IO.FileNotFoundException e)
                //    {
                //        // If file was deleted by a separate application
                //        //  or thread since the call to TraverseTree()
                //        // then just continue.
                //        Console.WriteLine(e.Message);
                //        continue;
                //    }
                //}

                // Push the subdirectories onto the stack for traversal.
                // This could also be done before handing the files.
                Parallel.ForEach(subDirs, str =>
                {
                    dirs.Push(str);
                });
            }
            Console.WriteLine("Icon and Splash generation finished");
            Console.WriteLine("Press any key to close");
            Console.ReadKey();
        }

        private static void CheckFile(FileInfo fi)
        {
            if (fi.Extension == ".png")
            {
                string iconOrSplashPath;
                if (fi.FullName.Contains("icon"))
                {
                    iconOrSplashPath = Properties.Settings.Default.iconPath;
                }
                else if (fi.FullName.Contains("splash") || fi.FullName.Contains("screen"))
                {
                    iconOrSplashPath = Properties.Settings.Default.splashPath;
                }
                else
                {
                    Console.WriteLine("No Icon or Splash: " + fi.FullName);
                    return;
                }
                if (fi.FullName == Properties.Settings.Default.iconPath || fi.FullName == Properties.Settings.Default.splashPath)
                {
                    return;
                }

                Console.WriteLine("Start replacing " + fi.Name);
                ReplaceFile(fi, iconOrSplashPath);
            }
        }

        private static void ReplaceFile(FileInfo fi, string iconOrSplashPath)
        {
            Bitmap imageToReplace;
            Bitmap newImage;
            try
            {
                newImage = new Bitmap(iconOrSplashPath);
                imageToReplace = new Bitmap(fi.FullName);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
            var width = imageToReplace.Width;
            var height = imageToReplace.Height;
            Bitmap result;
            if (width == height)
            {
                result = new Bitmap(newImage, new Size(width, height));
            }
            else
            {
                result = (Bitmap)FixedSize(newImage, width, height);
            }
            imageToReplace.Dispose();
            newImage.Dispose();
            try
            {
                result.Save(fi.FullName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
            Console.WriteLine("Done replacing " + fi.Name);
            result.Dispose();
        }

        private static Image FixedSize(Image imgPhoto, int Width, int Height)
        {
            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;
            int sourceX = 0;
            int sourceY = 0;
            int destX = 0;
            int destY = 0;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)Width / (float)sourceWidth);
            nPercentH = ((float)Height / (float)sourceHeight);
            if (nPercentH < nPercentW)
            {
                nPercent = nPercentH;
                destX = System.Convert.ToInt16((Width -
                              (sourceWidth * nPercent)) / 2);
            }
            else
            {
                nPercent = nPercentW;
                destY = System.Convert.ToInt16((Height -
                              (sourceHeight * nPercent)) / 2);
            }

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap bmPhoto = new Bitmap(Width, Height,
                              PixelFormat.Format24bppRgb);
            bmPhoto.SetResolution(imgPhoto.HorizontalResolution,
                             imgPhoto.VerticalResolution);

            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.InterpolationMode =
                    InterpolationMode.HighQualityBicubic;

            grPhoto.DrawImage(imgPhoto,
                new Rectangle(destX, destY, destWidth, destHeight),
                new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();
            bmPhoto.MakeTransparent();
            return bmPhoto;
        }
    }
}