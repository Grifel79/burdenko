using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
    public class ScreenCapture : MonoBehaviour
    {
        //Set your screenshot resolutions
        public int captureWidth;
        public int captureHeight;
        // configure with raw, jpg, png, or ppm (simple raw format)

        public string format;
        // folder to write output (defaults to data path)
        private string outputFolder;
        // private variables needed for screenshot
        private Rect rect;
        private RenderTexture renderTexture;
        private Texture2D screenShot;

        private bool isProcessing;

        void Start()
        {
            captureWidth = Screen.width;
            captureHeight = Screen.height;
            // configure with raw, jpg, png, or ppm (simple raw format)

            format = "PNG";

            outputFolder = "/Screenshots/";

            print(outputFolder);
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
                print("Save Path will be : " + outputFolder);
            }
        }

        private string CreateFileName(int width, int height)
        {
            //timestamp to append to the screenshot filename
            string timestamp = DateTime.Now.ToString("yyyyMMddTHHmmss");
            // use width, height, and timestamp for unique file 
            var filename = string.Format("{0}/screen_{1}x{2}_{3}.{4}", outputFolder, width, height, timestamp, format.ToString().ToLower());
            // return filename
            return filename;
        }

        private void CaptureScreenshot()
        {
            isProcessing = true;
            //// create screenshot objects
            //if (renderTexture == null)
            //{
            //    // creates off-screen render texture to be rendered into
            //    rect = new Rect(0, 0, captureWidth, captureHeight);
            //    renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
            //    screenShot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
            //}
            //// get main camera and render its output into the off-screen render texture created above
            //Camera camera = Camera.main;
            //camera.targetTexture = renderTexture;
            //camera.Render();
            //// mark the render texture as active and read the current pixel data into the Texture2D
            //RenderTexture.active = renderTexture;
            //screenShot.ReadPixels(rect, 0, 0);
            //// reset the textures and remove the render texture from the Camera since were done reading the screen data
            //camera.targetTexture = null;
            //RenderTexture.active = null;
            //// get our filename
            //string filename = CreateFileName((int)rect.width, (int)rect.height);
            //// get file header/data bytes for the specified image format
            //byte[] fileHeader = null;
            //byte[] fileData = null;
            ////Set the format and encode based on it

            //fileData = screenShot.EncodeToPNG();

            //print(filename);

            //// create new thread to offload the saving from the main thread
            //new System.Threading.Thread(() =>
            //{

            //    print("i am in the thread");
            //    var file = System.IO.File.Create(filename);
            //    if (fileHeader != null)
            //    {
            //        file.Write(fileHeader, 0, fileHeader.Length);
            //    }
            //    file.Write(fileData, 0, fileData.Length);
            //    file.Close();
            //    print(string.Format("Screenshot Saved {0}, size {1}", filename, fileData.Length));
            //    isProcessing = false;
            //}).Start();
            ////Cleanup
            ///
            string filename = CreateFileName((int)rect.width, (int)rect.height);
            print(filename);
            Application.CaptureScreenshot("C:\\" + filename);
            isProcessing = false;

        }

        void Update()
        {
            if (!isProcessing)
            {
                CaptureScreenshot();
            }
            else
            {
                print("Currently Processing");
            }
        }

    }
}
