using System;
using System.Windows.Forms;

namespace CodemastersBinImage
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            /*
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length >= 2) // mode, files, {log, rom}
            {
                if (!File.Exists(args[2])) return; // files

                string[] files = File.ReadAllLines(args[2]);

                char mode = args[1].ToLower()[0]; // mode

                if (mode == 'e')
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        bool isBitmap;
                        try
                        {
                            isBitmap = (Bitmap.FromFile(files[i]) != null);
                        }
                        catch
                        {
                            isBitmap = false;
                        }

                        BinImageData bid = BinImageData.FromFile(files[i]);

                        if (bid == null) continue;

                        if (!isBitmap)
                        {
                            Bitmap bmp = bid.Image;

                            if (bmp == null) continue;

                            bmp.Save(Path.GetFileName(Path.ChangeExtension(files[i], ".bmp")), ImageFormat.Bmp);
                            bmp.Dispose();
                        }
                        else
                        {
                            File.WriteAllBytes(Path.GetFileName(Path.ChangeExtension(files[i], ".imp.bin")), bid.Data);
                        }
                    }
                }
                else if (mode == 'i')
                {
                    if (!File.Exists(args[4])) return; // rom
                    if (!File.Exists(args[3])) return; // log

                    byte[] rom = File.ReadAllBytes(args[4]);
                    string[] log = File.ReadAllLines(args[3]);

                    List<int> diffs = new List<int>(log.Length);

                    Regex regex = new Regex(@"^([A-Fa-f0-9]{8}) \(([A-Fa-f0-9]{4})");
                    for (int i = 0; i < log.Length; i++)
                    {
                        Match match = regex.Match(log[i]);

                        if (match.Success)
                        {
                            int size = Convert.ToInt32(match.Groups[2].Value, 16);

                            BinImageData bid = BinImageData.FromFile(files[i]);

                            byte[] impData = bid.Data;

                            int diff = (impData.Length - size) + ((i > 0) ? diffs[i - 1] : 0);
                            Console.WriteLine("[{0:000}, {1}]: {2}", i, files[i], diff);

                            diffs.Add(diff);
                        }
                    }

                    int firstOffset = 0, newOffset = 0;
                    for (int i = 0; i < log.Length; i++)
                    {
                        Match match = regex.Match(log[i]);

                        if (match.Success)
                        {
                            int offset = Convert.ToInt32(match.Groups[1].Value, 16);
                            int size = Convert.ToInt32(match.Groups[2].Value, 16);

                            firstOffset = ((firstOffset == 0) ? offset : firstOffset);

                            BinImageData bid = BinImageData.FromFile(files[i]);

                            byte[] impData = bid.Data;

                            newOffset = offset + ((i > 0) ? diffs[i - 1] : 0);

                            for (int k = 0; k < (0xD089C - newOffset); ++k)
                            {
                                rom[newOffset + k] = 0x00; // clear old imps
                            }

                            Array.Copy(impData, 0, rom, newOffset, impData.Length);

                            if (i > 0)
                            {
                                List<int> refs = new List<int>();

                                int refFrom = -1;
                                while ((refFrom = Helpers.SearchBytes(
                                    rom,
                                    new byte[] {
                                        (byte)((offset >> 24) & 0xFF),
                                        (byte)((offset >> 16) & 0xFF),
                                        (byte)((offset >> 8) & 0xFF),
                                        (byte)((offset >> 0) & 0xFF),
                                    }, refFrom + 1
                                    )) != -1)
                                {
                                    if (refFrom >= firstOffset) continue;
                                    refs.Add(refFrom);
                                }

                                for (int j = 0; j < refs.Count; ++j)
                                {
                                    Console.WriteLine(string.Format("Fixing ref {0:X6}: {1:X6} -> {2:X6}", refs[j], offset, newOffset));
                                    rom.WriteLong(refs[j], (uint)newOffset);
                                }
                            }
                        }
                    }
                    File.WriteAllBytes(Path.ChangeExtension(args[4], ".new.bin"), rom);
                }

                return;
            }
            */

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
        }
    }
}
