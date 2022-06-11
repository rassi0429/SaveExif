extern alias lib1;
using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Threading.Tasks;
using System.IO;
using MimeDetective;
using CodeX;
using BaseX;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace SaveExif
{
    public class SaveExif : NeosMod
    {
        public override string Name => "SaveExif";
        public override string Author => "kka429";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/rassi0429/SaveExif"; // this line is optional and can be omitted

        // Exif Type:  https://github.com/mono/libgdiplus/blob/main/src/gdiplusimaging.h

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("dev.kokoa.saveexif");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(lib1::WindowsPlatformConnector), "NotifyOfScreenshot", new Type[] { typeof(World), typeof(string), typeof(ScreenshotType), typeof(DateTime) })]
        class Patch
        {

            private static void SetProperty(ref System.Drawing.Imaging.PropertyItem prop, int iId, string sTxt)
            {
                int iLen = sTxt.Length + 1;
                byte[] bTxt = new Byte[iLen];
                for (int i = 0; i < iLen - 1; i++)
                    bTxt[i] = (byte)sTxt[i];
                bTxt[iLen - 1] = 0x00;
                prop.Id = iId;
                prop.Type = 2;
                prop.Value = bTxt;
                prop.Len = iLen;
            }

            static bool Prefix(bool ___keepOriginalScreenshotFormat, lib1::WindowsPlatformConnector __instance, World world, string file, ScreenshotType type, DateTime timestamp)
            {
                __instance.Engine.GlobalCoroutineManager.StartTask((Func<Task>)(async () =>
                {
                    await new ToBackground();
                    string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    pictures = Path.Combine(pictures, "Neos VR");
                    Directory.CreateDirectory(pictures);
                    string filename = timestamp.ToLocalTime().ToString("yyyy-MM-dd HH.mm.ss"); //FIX LOCALTIME
                    string extension = ___keepOriginalScreenshotFormat ? Path.GetExtension(file) : ".jpg";
                    if (string.IsNullOrWhiteSpace(extension))
                    {
                        FileType fileType = new FileInfo(file).GetFileType();
                        if (fileType != null)
                            extension = "." + fileType.Extension;
                    }
                    await lib1::WindowsPlatformConnector.ScreenshotSemaphore.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        int num = 1;
                        string str1;
                        do
                        {
                            string str2 = filename;
                            if (num > 1)
                                str2 += string.Format(" ({0})", (object)num);
                            str1 = Path.Combine(pictures, str2 + extension);
                            ++num;
                        }
                        while (File.Exists(str1));
                        if (___keepOriginalScreenshotFormat)
                        {
                            File.Copy(file, str1);
                            File.SetAttributes(str1, FileAttributes.Normal);
                        }
                        else
                        {
                            TextureEncoder.ConvertToJPG(file, file + ".tmp");
                            Image img = Image.FromFile(file + ".tmp");

                            PropertyItem w = img.PropertyItems[0];
                            SetProperty(ref w, 272, "NeosCamera");
                            img.SetPropertyItem(w);
                            PropertyItem w2 = img.PropertyItems[0];
                            SetProperty(ref w2, 271, "FrooxEngine");
                            img.SetPropertyItem(w2);
                            PropertyItem w4 = img.PropertyItems[0];
                            w4.Id = 0x9003; //撮影日時
                            w4.Type = 2;
                            w4.Len = 20;
                            w4.Value = System.Text.Encoding.ASCII.GetBytes(timestamp.ToLocalTime().ToString("yyyy:MM:dd HH:mm:ss"));
                            img.SetPropertyItem(w4);

                            PropertyItem w5 = img.PropertyItems[0];
                            w5.Id = 0x9286; //COMMENT
                            w5.Type = 7;

                            var locationName = Engine.Current.WorldManager.FocusedWorld.GetSessionInfo().Name;
                            var locationUrl = Engine.Current.WorldManager.FocusedWorld.IsPublic ? Engine.Current.WorldManager.FocusedWorld.CorrespondingRecord?.URL.ToString() : "private";
                            var hostUserId = Engine.Current.WorldManager.FocusedWorld.HostUser.UserID;
                            var hostUserName = Engine.Current.WorldManager.FocusedWorld.HostUser.UserName;
                            var timeTaken = timestamp.ToString();
                            var takeUserId = Engine.Current.WorldManager.FocusedWorld.LocalUser.UserID;
                            var takeUserName = Engine.Current.WorldManager.FocusedWorld.LocalUser.UserName;
                            var neosVersion = Engine.Version.ToString();
                            var _presentUser = Engine.Current.WorldManager.FocusedWorld.AllUsers;
                            List<string> presentUserIdArray = new List<string>();
                            List<string> presentUserNameArray = new List<string>();
                            foreach(var user in _presentUser)
                            {
                                presentUserIdArray.Add(user.UserID);
                                presentUserNameArray.Add(user.UserName);
                            }

                            string str = $"{{\"locationName\":\"{locationName}\",\n" +
                            $"\"locationUrl\":\"{locationUrl}\",\n" +
                            $"\"hostUserId\":\"{hostUserId}\",\n" +
                            $"\"hostUserName\":\"{hostUserName}\",\n" +
                            $"\"timeTaken\":\"{timeTaken}\",\n" +
                            $"\"takeUserId\":\"{takeUserId}\",\n" +
                            $"\"takeUserName\":\"{takeUserName}\",\n" +
                            $"\"neosVersion\":\"{neosVersion}\",\n" +
                            $"\"takeUserName\":\"{takeUserName}\",\n" +
                            $"\"presentUserIdArray\":[\"{String.Join("\",\"", presentUserIdArray)}\"],\n" +
                            $"\"presentUserNameArray\":[\"{String.Join("\",\"", presentUserNameArray)}\"]}}";

                            byte[] header = { 0x55, 0x4E, 0x49, 0x43, 0x4F, 0x44, 0x45, 0x0 };
                            byte[] content = System.Text.Encoding.Unicode.GetBytes(str);
                            byte[] main = new byte[header.Length + content.Length];
                            Array.Copy(header, main, header.Length);
                            Array.Copy(content, 0, main, header.Length, content.Length);
                            w5.Len = main.Length;
                            w5.Value = main;
                            img.SetPropertyItem(w5);

                            PropertyItem w6 = img.PropertyItems[0];
                            w6.Id = 0x010E; //TITLE
                            w6.Type = 2;
                            w6.Len = 11;
                            w6.Value = System.Text.Encoding.ASCII.GetBytes("Neos Photo\0");
                            img.SetPropertyItem(w6);

                            PropertyItem w7 = img.PropertyItems[0];
                            w7.Id = 0x013B; //ARTIST
                            w7.Type = 2;
                            var username = Engine.Current.LocalUserName;
                            w7.Len = username.Length + 1;
                            w7.Value = System.Text.Encoding.ASCII.GetBytes(username + "\0");
                            img.SetPropertyItem(w7);

                            PropertyItem w8 = img.PropertyItems[0];
                            w8.Id = 0x0131; //ARTIST
                            w8.Type = 2;
                            w8.Len = 7;
                            w8.Value = System.Text.Encoding.ASCII.GetBytes("NeosVR\0");
                            img.SetPropertyItem(w8);

                            img.Save(str1);
                            img.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        UniLog.Error("Exception saving screenshot to Windows:\n" + (object)ex);
                    }
                    finally
                    {
                        lib1::WindowsPlatformConnector.ScreenshotSemaphore.Release();
                    }
                }));


                return false;
                // __instance.ForEachConnector((Action<IPlatformConnector>)(c => Msg(c.GetType().Assembly)));

                //Msg(file
                //string[] filename = file.Split('\\');
                //if (!filename[filename.Length - 1].Contains("jpg")) return;
                //File.Move(file, file + "d");
                //Image img = Image.FromFile(file + "d");
                //System.Drawing.Imaging.PropertyItem w = img.PropertyItems[0];
                //Msg(w);
                //w = img.PropertyItems[0];
                //SetProperty(ref w, 272, "NeosVR kokopi MOD");
                //img.SetPropertyItem(w);
                //img.Save(file);
                //img.Dispose();

                // File.Copy(file, @"C:\Users\neo.KOKOA\Documents\k\" +  filename[filename.Length - 1]);
                // return true;
                //FIBITMAP freeImage = __instance.ToFreeImage();
                //MetadataTag tag = new MetadataTag(FREE_IMAGE_MDMODEL.FIMD_COMMENTS);
                //tag = new MetadataTag(FREE_IMAGE_MDMODEL.FIMD_COMMENTS);
                //tag.Key = "KEY1";
                //tag.Value = 12345;
                //tag.AddToImage(freeImage);

                //tag = new MetadataTag(FREE_IMAGE_MDMODEL.FIMD_COMMENTS);
                //tag.Key = "KEY2";
                //tag.Value = 54321;
                //tag.AddToImage(freeImage);
                //Msg("ok");
                //Msg(extension);
                //try
                //{
                //    __result = TextureEncoder.Encode(freeImage, stream, extension, quality, preserveColorInAlpha);
                //}
                //finally
                //{
                //    FreeImage.Unload(freeImage);
                //}
                //return false;
                // Msg(FrooxEngine.Engine.Current.WorldManager.FocusedWorld.Name);
            }
        }
    }
}