using System;
using System.ComponentModel;
using System.IO;

using Vintasoft.Twain;

namespace TwainConsoleDemo
{
    class Program
    {

        static void Main(string[] args)
        {
            try
            {
                VintasoftTwain.VintasoftTwainLicense.Register();

                // create TWAIN device manager
                using (DeviceManager deviceManager = new DeviceManager())
                {
                    // try to use TWAIN device manager 2.x
                    deviceManager.IsTwain2Compatible = true;
                    // if TWAIN device manager 2.x is not available
                    if (!deviceManager.IsTwainAvailable)
                    {
                        // try to use TWAIN device manager 1.x
                        deviceManager.IsTwain2Compatible = false;
                        // if TWAIN device manager 1.x is not available
                        if (!deviceManager.IsTwainAvailable)
                        {
                            Console.WriteLine("TWAIN device manager is not available.");
                            return;
                        }
                    }

                    // if 64-bit TWAIN2 device manager is used
                    if (IntPtr.Size == 8 && deviceManager.IsTwain2Compatible)
                    {
                        Console.Write("Use 32-bit devices in 64-bit TWAIN2 device manager (Y = yes, N = no)? ");
                        char key = Console.ReadKey().KeyChar;

                        if (key == 'y' || key == 'Y')
                            deviceManager.Use32BitDevices();
                        Console.WriteLine();
                    }

                    // open the device manager
                    deviceManager.Open();

                    // if no devices are found in the system
                    if (deviceManager.Devices.Count == 0)
                    {
                        Console.WriteLine("Devices are not found.");
                        return;
                    }

                    // select the device
                    deviceManager.ShowDefaultDeviceSelectionDialog();

                    // get reference to the current device
                    Device device = deviceManager.DefaultDevice;

                    device.ShowUI = false;
                    device.DisableAfterAcquire = true;

                    // open the device
                    device.Open();

                    // set acquisition parameters
                    if (device.TransferMode != TransferMode.Native)
                        device.TransferMode = TransferMode.Native;
                    if (device.PixelType != PixelType.BW)
                        device.PixelType = PixelType.BW;

                    // create directory for TIFF file
                    string directoryForImages = Path.GetDirectoryName(Directory.GetCurrentDirectory());
                    directoryForImages = Path.Combine(directoryForImages, "Images");
                    if (!Directory.Exists(directoryForImages))
                        Directory.CreateDirectory(directoryForImages);

                    string multipageTiffFilename = Path.Combine(directoryForImages, "multipage.tif");

                    // acquire image(s) from the device
                    int imageIndex = 0;
                    AcquireModalState acquireModalState = AcquireModalState.None;
                    do
                    {
                        acquireModalState = device.AcquireModal();
                        switch (acquireModalState)
                        {
                            case AcquireModalState.ImageAcquired:
                                // save acquired image to a file
                                device.AcquiredImage.Save(multipageTiffFilename);
                                // dispose an acquired image
                                device.AcquiredImage.Dispose();

                                Console.WriteLine(string.Format("Image{0} is saved.", imageIndex++));
                                break;

                            case AcquireModalState.ScanCompleted:
                                Console.WriteLine("Scan is completed.");
                                break;

                            case AcquireModalState.ScanCanceled:
                                Console.WriteLine("Scan is canceled.");
                                break;

                            case AcquireModalState.ScanFailed:
                                Console.WriteLine(string.Format("Scan is failed: {0}", device.ErrorString));
                                break;

                            case AcquireModalState.UserInterfaceClosed:
                                Console.WriteLine("User interface is closed.");
                                break;
                        }
                    }
                    while (acquireModalState != AcquireModalState.None);

                    // close the device
                    device.Close();

                    // close the device manager
                    deviceManager.Close();
                }
            }
            catch (TwainException ex)
            {
                Console.WriteLine("Error: " + GetFullExceptionMessage(ex));
            }
            catch (Exception e)
            {
                System.ComponentModel.LicenseException licenseException = GetLicenseException(e);
                if (licenseException != null)
                {
                    // show information about licensing exception
                    Console.WriteLine("{0}: {1}", licenseException.GetType().Name, licenseException.Message);

                    string[] dirs = new string[] { ".", "..", @"..\..\", @"..\..\..\", @"..\..\..\..\..\", @"..\..\..\..\..\..\..\" };
                    // for each directory
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        string filename = System.IO.Path.Combine(dirs[i], "VSTwainNetEvaluationLicenseManager.exe");
                        // if VintaSoft Evaluation License Manager exists in directory
                        if (System.IO.File.Exists(filename))
                        {
                            // start Vintasoft Evaluation License Manager for getting the evaluation license
                            System.Diagnostics.Process process = new System.Diagnostics.Process();
                            process.StartInfo.FileName = filename;
                            process.Start();
                        }
                    }
                }
                else
                {
                    throw;
                }
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Returns the message of exception and inner exceptions.
        /// </summary>
        private static string GetFullExceptionMessage(Exception ex)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine(ex.Message);

            Exception innerException = ex.InnerException;
            while (innerException != null)
            {
                if (ex.Message != innerException.Message)
                    sb.AppendLine(string.Format("Inner exception: {0}", innerException.Message));
                innerException = innerException.InnerException;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns the license exception from specified exception.
        /// </summary>
        /// <param name="exceptionObject">The exception object.</param>
        /// <returns>Instance of <see cref="LicenseException"/>.</returns>
        private static System.ComponentModel.LicenseException GetLicenseException(object exceptionObject)
        {
            Exception ex = exceptionObject as Exception;
            if (ex == null)
                return null;
            if (ex is System.ComponentModel.LicenseException)
                return (System.ComponentModel.LicenseException)exceptionObject;
            if (ex.InnerException != null)
                return GetLicenseException(ex.InnerException);
            return null;
        }

    }
}