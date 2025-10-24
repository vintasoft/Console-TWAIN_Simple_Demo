using System;
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
            // 0 - 32-bit TWAIN1 device manager; 1 - 32-bit TWAIN2 device manager; 2 - 64-bit TWAIN2 device manager.
            int twainDeviceManagerType = -1;

            char key;
            // if application is running on 32-bit Windows
            if (IntPtr.Size == 4)
            {
                Console.Write("Do you want to use 32-bit TWAIN1 device manager (press '1') or 32-bit TWAIN2 device manager (press '2')? ");
                do
                {
                    key = Console.ReadKey().KeyChar;
                }
                while (key != '1' && key != '2');
                if (key == '1')
                    twainDeviceManagerType = 0;
                else
                    twainDeviceManagerType = 1;
            }
            // if application is running on 64-bit Windows or Linux
            else
            {
                // if application is running on 64-bit Windows
                if (TwainEnvironment.OsPlatform == OsPlatform.Windows)
                {
                    Console.Write("Do you want to use 32-bit TWAIN2 device manager (press '1') or 64-bit TWAIN2 device manager (press '2')? ");
                    do
                    {
                        key = Console.ReadKey().KeyChar;
                    }
                    while (key != '1' && key != '2');
                    if (key == '1')
                        twainDeviceManagerType = 1;
                    else
                        twainDeviceManagerType = 2;
                }
                // if application is running on 64-bit Linux
                else
                {
                    twainDeviceManagerType = 2;
                }
            }

            string twainDsmDllCustomPath;
            switch (twainDeviceManagerType)
            {
                case 0:
                    // try to use 32-bit TWAIN1 device manager
                    deviceManager.IsTwain2Compatible = false;
                    break;

                case 1:
                    // get path to the TWAIN device manager 2.x from installation of VintaSoft TWAIN .NET SDK
                    twainDsmDllCustomPath = GetTwainDsmCustomPath(true);
                    // if file exist
                    if (twainDsmDllCustomPath != null)
                        // specify that SDK should use TWAIN device manager 2.x from installation of VintaSoft TWAIN .NET SDK
                        deviceManager.TwainDllPath = twainDsmDllCustomPath;

                    deviceManager.IsTwain2Compatible = true;

                    // if application is running on 64-bit Windows
                    if (IntPtr.Size == 8)
                    { 
                        deviceManager.Use32BitDevices();
                    }
                    break;

                case 2:
                    // get path to the TWAIN device manager 2.x from installation of VintaSoft TWAIN .NET SDK
                    twainDsmDllCustomPath = GetTwainDsmCustomPath(false);
                    // if file exist
                    if (twainDsmDllCustomPath != null)
                        // specify that SDK should use TWAIN device manager 2.x from installation of VintaSoft TWAIN .NET SDK
                        deviceManager.TwainDllPath = twainDsmDllCustomPath;

                    // try to use 64-bit TWAIN2 device manager
                    deviceManager.IsTwain2Compatible = true;
                    break;
            }

            // if TWAIN device manager is not available
            if (!deviceManager.IsTwainAvailable)
            {
                Console.WriteLine("TWAIN device manager is not available.");
                return false;
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
                Console.Write(string.Format("Please select device by entering the device number from '1' to '{0}' ('0' to cancel) and press 'Enter' key: ", deviceCount));
                string deviceIndexString = Console.ReadLine();
                int.TryParse(deviceIndexString, out deviceIndex);
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

        /// <summary>
        /// Returns path to the TWAIN device manager 2.x from installation of VintaSoft TWAIN .NET SDK.
        /// </summary>
        /// <param name="use32BitDevice">The value indicating whether the 32-bit TWAIN device must be used.</param>
        /// <returns>The path to the TWAIN device manager 2.x from installation of VintaSoft TWAIN .NET SDK.</returns>
        private static string GetTwainDsmCustomPath(bool use32BitDevice)
        {
            string twainFolderName = "TWAINDSM64";
            if (use32BitDevice)
                twainFolderName = "TWAINDSM32";

            string[] binFolderPaths = { @"..\..\Bin", @"..\..\..\..\..\Bin", @"..\..\..\..\..\..\Bin" };
            string binFolderPath = null;
            for (int i = 0; i < binFolderPaths.Length; i++)
            {
                if (Directory.Exists(Path.Combine(binFolderPaths[i], twainFolderName)))
                {
                    binFolderPath = binFolderPaths[i];
                    break;
                }
            }

            if (binFolderPath != null)
            {
                if (use32BitDevice)
                    // get path to the TWAIN device manager 2.x (32-bit) from installation of VintaSoft TWAIN .NET SDK
                    return Path.Combine(binFolderPath, "TWAINDSM32", "TWAINDSM.DLL");
                else
                    // get path to the TWAIN device manager 2.x (64-bit) from installation of VintaSoft TWAIN .NET SDK
                    return Path.Combine(binFolderPath, "TWAINDSM64", "TWAINDSM.DLL");
            }

            return null;
        }

    }
}
