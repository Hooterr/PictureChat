using ImageMagick;
using System;
using System.Drawing;
using System.IO;
using System.Text;

namespace PictureChat
{
    class Program
    {
        private const uint imageStartSequence = 0xFFDA;
        private static byte reverse(byte b)
        {
            b = (byte)((b & 0xF0) >> 4 | ((b & 0x0F) << 4));
            b = (byte)((b & 0xCC) >> 2 | (b & 0x33) << 2);
            b = (byte)((b & 0xAA) >> 1 | (b & 0x55) << 1);
            return b;
        }
        private static Bitmap CreateNonIndexedImage(Image src)
        {
            Bitmap newBmp = new Bitmap(src.Width, src.Height, src.PixelFormat);

            using (Graphics gfx = Graphics.FromImage(newBmp))
            {
                gfx.DrawImage(src, 0, 0);
            }

            return newBmp;
        }
        static void Main(string[] args)
        {
            Console.WriteLine("");
            if (args.Length > 0)
            {
                var operation = OperationDeterminer.GetOperation(args[0]);
                switch(operation)
                {
                    case Operation.Decrypt:
                        if (args.Length > 1)
                        {
                            var inPath = args[1];
                            var sb = new StringBuilder();
                            var found = false;
                            if (Path.GetExtension(inPath) == ".png")
                            {
                                if (File.Exists(inPath))
                                {
                                    using (var bitmap = (Bitmap)Image.FromFile(inPath))
                                    {
                                        byte counter = 0x01;
                                        byte currentChar = 0;
                                        for (int x = 0; x < bitmap.Width; x++)
                                        {
                                            for (int y = 0; y < bitmap.Height; y += 2)
                                            {
                                                var pixel = bitmap.GetPixel(x, y);

                                                currentChar |= (byte)(pixel.A % 2);

                                                counter <<= 1;
                                                if (counter == 0)
                                                {
                                                    counter = 0x01;
                                                    var currentCharStr = Encoding.UTF8.GetString(new byte[] { reverse(currentChar) });

                                                    if (currentCharStr == "\0")
                                                    {
                                                        found = true;
                                                        break;
                                                    }

                                                    sb.Append(currentCharStr);
                                                    currentChar = 0;
                                                }
                                                currentChar <<= 1;

                                            }
                                            if (found)
                                                break;
                                        }

                                        if (found)
                                        {
                                            var messageDecrypted = sb.ToString();
                                            Console.WriteLine($"Decoded message: {messageDecrypted}");
                                        }
                                        else
                                        {
                                            Console.WriteLine("No meaningful message was found in this file");
                                        }

                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"File not found");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Unsupported file type. Only supported type is png for now.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Specify input file path");
                        }

                        break;

                    case Operation.Encrypt:

                        if (args.Length > 3)
                        {
                            var inputPath = args[1];
                            var message = args[2] + '\0';
                            var encryptedTextBytes = Encoding.UTF8.GetBytes(message);
                            var finished = false;
                            if (Path.GetExtension(inputPath) == ".png")
                            {
                                if (File.Exists(inputPath))
                                {
                                    using (var image = Image.FromFile(inputPath))
                                    {
                                        using (var bitmap = (Bitmap)image)
                                        {
                                            var encryptedTextBytesIdx = 0;
                                            byte counterMask = 0x01;
                                            for (int x = 0; x < bitmap.Width; x++)
                                            {
                                                for (int y = 0; y < bitmap.Height; y += 2)
                                                {
                                                    var currentPixel = bitmap.GetPixel(x, y);
                                                    if (currentPixel.A > 0)
                                                    {
                                                        var shouldMod2Be0 = (encryptedTextBytes[encryptedTextBytesIdx] & counterMask) == 0;
                                                        var isMod2Eq0 = currentPixel.A % 2 == 0;
                                                        if (shouldMod2Be0 != isMod2Eq0)
                                                        {
                                                            var newColor = Color.FromArgb(currentPixel.A + (currentPixel.A == 0 ? 1 : -1), currentPixel.R, currentPixel.G, currentPixel.B);
                                                            bitmap.SetPixel(x, y, newColor);
                                                        }
                                                        counterMask <<= 1;
                                                        if (counterMask == 0)
                                                        {
                                                            counterMask = 0x01;
                                                            encryptedTextBytesIdx++;
                                                            if (encryptedTextBytesIdx >= encryptedTextBytes.Length)
                                                            {
                                                                var fileFormat = bitmap.RawFormat;
                                                                var outputPath = args.Length >= 4 ? args[3] : inputPath;
                                                                bitmap.Save(outputPath, bitmap.GetImageFormat());
                                                                finished = true;
                                                                break;
                                                            }

                                                        }
                                                    }
                                                }
                                                if (finished)
                                                    break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"File not found");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Unsupported file type. Only supported type is png for now.");
                            }
                        }
                        else
                        {
                            switch (args.Length)
                            {
                                case 1:
                                    Console.WriteLine("Specify input file path");
                                    break;
                                case 2:
                                    Console.WriteLine("Specify the message to encrypt");
                                    break;
                                case 3:
                                    Console.WriteLine("Specify output file name");
                                    break;
                            }
                        }
                        
                        break;

                    case Operation.Unknown:
                        PrintInfoStuff();
                        break;

                }
            }
            else
            {
                PrintInfoStuff();
            }
        }
        public static void PrintInfoStuff()
        {
            Console.WriteLine($"Usage:  dotnet PictureChat [options]");
            Console.WriteLine($"Options:");
            Console.WriteLine($" -encrypt [inputFile] [message] [outputfile]");
            Console.WriteLine($" -decrypt [inputFile]");
            Console.WriteLine($"\nOnly supported type is png for the moment");
        }
    }
}
