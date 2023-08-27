using System;
using System.IO;

using Vintasoft.Twain;

namespace TwainConsoleDemo
{
    class Program
    {

        static void Main(string[] args)
        {
            // register the evaluation license for VintaSoft TWAIN .NET SDK
            Vintasoft.Twain.TwainGlobalSettings.Register("REG_USER", "REG_EMAIL", "EXPIRATION_DATE", "REG_CODE");

            try
            {
                // create TWAIN device manager
                using (DeviceManager deviceManager = new DeviceManager())
                {
                    // open TWAIN device manager
                    if (!OpenDeviceManager(deviceManager))
                        return;

                    // select TWAIN device
                    Device device = SelectDevice(deviceManager);
                    // if device is not selected
                    if (device == null)
                        return;

                    // specify that device UI should not be shown
                    device.ShowUI = false;
                    // specify that image scanning progess UI should not be shown
                    device.ShowIndicators = false;
                    // specify that device must be disabled after image scan
                    device.DisableAfterAcquire = true;

                    // open the device
                    device.Open();

                    // set device parameters
                    device.TransferMode = TransferMode.Native;
                    device.PixelType = PixelType.BW;
                    device.XferCount = 1;

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

                    // open article with information about usage of evaluation license
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = "https://www.vintasoft.com/docs/vstwain-dotnet/Licensing-Twain-Evaluation.html";
                    process.StartInfo.UseShellExecute = true;
                    process.Start();
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
        /// Opens the TWAIN device manager.
        /// </summary>
        /// <param name="deviceManager">Device manager.</param>
        /// <returns><b>True</b> - device manager is opened successfully; otherwise, <b>false</b>.</returns>
        private static bool OpenDeviceManager(DeviceManager deviceManager)
        {
            // try to use TWAIN device manager 2.x
            deviceManager.IsTwain2Compatible = true;
            // if TWAIN device manager 2.x is not available
            if (!deviceManager.IsTwainAvailable)
            {
                // if application is executed in Windows
                if (TwainEnvironment.OsPlatform == OsPlatform.Windows)
                {
                    // try to use TWAIN device manager 1.x
                    deviceManager.IsTwain2Compatible = false;
                    // if TWAIN device manager 1.x is not available
                    if (!deviceManager.IsTwainAvailable)
                    {
                        Console.WriteLine("TWAIN device manager is not available.");
                        return false;
                    }
                }
                // if application is executed in Linux or macOS
                else
                {
                    Console.WriteLine("TWAIN device manager is not available.");
                    return false;
                }
            }

            char key;
            // if application is executed in Windows
            if (TwainEnvironment.OsPlatform == OsPlatform.Windows)
            {
                // if 64-bit TWAIN2 device manager is used
                if (IntPtr.Size == 8 && deviceManager.IsTwain2Compatible)
                {
                    Console.Write("Do you want to use 32-bit devices in 64-bit TWAIN2 device manager (Y = yes, N = no)? ");
                    key = Console.ReadKey().KeyChar;

                    if (key == 'y' || key == 'Y')
                        deviceManager.Use32BitDevices();
                    Console.WriteLine();
                    Console.WriteLine();
                }
            }

            // open the device manager
            deviceManager.Open();

            return true;
        }

        /// <summary>
        /// Selects the TWAIN device.
        /// </summary>
        /// <param name="deviceManager">TWAIN device manager.</param>
        /// <returns>TWAIN device if device is selected; otherwiase, <b>null</b>.</returns>
        private static Device SelectDevice(DeviceManager deviceManager)
        {
            int deviceCount = deviceManager.Devices.Count;
            // if no devices are found in the system
            if (deviceCount == 0)
            {
                Console.WriteLine("Devices are not found.");
                return null;
            }

            Console.WriteLine("Device list:");
            for (int i = 0; i < deviceCount; i++)
            {
                Console.WriteLine(string.Format("{0}. {1}", i + 1, deviceManager.Devices[i].Info.ProductName));
            }

            int deviceIndex = -1;
            while (deviceIndex < 0 || deviceIndex > deviceCount)
            {
                Console.Write(string.Format("Please select device by entering the device number from '1' to '{0}' or press '0' to cancel: ", deviceCount));
                deviceIndex = Console.ReadKey().KeyChar - '0';
                Console.WriteLine();
            }
            Console.WriteLine();

            if (deviceIndex == 0)
                return null;

            return deviceManager.Devices[deviceIndex - 1];
        }

        /// <summary>
        /// Returns message that contains information about exception and inner exceptions.
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
