// Portions Copyright (c) Secret Labs LLC.

using System;
namespace Microsoft.NetMicroFramework.Tools.MFDeployTool.Properties
{
	internal static class Resources
	{
		public static string ApplicationTitle
		{
			get
			{
				return ".NET Micro Framework Deployment Tool";
			}
		}
		
		public static string ButtonApply
		{
			get
			{
				return "&Apply";
			}
		}
		
		
		public static string ButtonBrowse
		{
			get
			{
				return "&Browse...";
			}
		}
		
		public static string ButtonBrowseDotDotDot
		{
			get
			{
				return "...";
			}
		}
		
		public static string ButtonCancel
		{
			get
			{
				return "&Cancel";
			}
		}
		
		public static string ButtonClear
		{
			get
			{
				return "&Clear";
			}
		}
		
		public static string ButtonDeploy
		{
			get
			{
				return "&Deploy";
			}
		}
		
		public static string ButtonErase
		{
			get
			{
				return "&Erase";
			}
		}
		
		public static string ButtonPing
		{
			get
			{
				return "&Ping";
			}
		}
		
		public static string ButtonUpdate
		{
			get
			{
				return "&Update";
			}
		}
		
		public static string CheckBoxEnable
		{
			get
			{
				return "&Enable";
			}
		}
		
		public static string ColumnHeaderBaseAddr
		{
			get
			{
				return "Base Address";
			}
		}
		
		public static string ColumnHeaderFile
		{
			get
			{
				return "File";
			}
		}
		
		public static string ColumnHeaderSize
		{
			get
			{
				return "Size";
			}
		}
		
		public static string ColumnHeaderTimeStamp
		{
			get
			{
				return "Time Stamp";
			}
		}
		
		public static string Connected
		{
			get
			{
				return "Connected";
			}
		}
		
		public static string ConnectingToX
		{
			get
			{
				return "Connecting to {0}...";
			}
		}
		
		public static string CopyrightWarning
		{
			get
			{
				return "Warning: This computer program is protected by copyright law and international treaties. Unauthorized reproduction or distribution of this program, or any portion of it, may result in severe civil and criminal penalties, and will be prosecuted to the maximum extent possible under the law.";
			}
		}
		
		public static string CreateDeploymentTitle
		{
			get
			{
				return "Create Deployment File As";
			}
		}
		
		public static string DefaultGateway
		{
			get
			{
				return "Default Gateway";
			}
		}
		
		public static string DeploymentStatusTitleErase
		{
			get
			{
				return "Erase Deployment Status";
			}
		}
		
		public static string Error
		{
			get
			{
				return "Error";
			}
		}
		
		public static string ErrorAppSettings
		{
			get
			{
				return "Error: Invalid Application Settings: {0}";
			}
		}
		
		public static string ErrorArgumentsInvalid
		{
			get
			{
				return "Invalid argument(s): ";
			}
		}
		
		public static string ErrorCommandMissing
		{
			get
			{
				return "The COMMAND parameter is missing";
			}
		}
		
		public static string ErrorCommandsMultiple
		{
			get
			{
				return "Only one COMMAND parameter is valid";
			}
		}
		
		public static string ErrorDeployment
		{
			get
			{
				return "Unable to deploy to device";
			}
		}
		
		public static string ErrorDeviceNotResponding
		{
			get
			{
				return "Failure - Device {0} is invalid or not responding";
			}
		}
		
		public static string ErrorErase
		{
			get
			{
				return "Unable to erase device";
			}
		}
		
		public static string ErrorFailure
		{
			get
			{
				return "Failure - {0}";
			}
		}
		
		public static string ErrorFileCantOpen
		{
			get
			{
				return "The file {0} could not be opened";
			}
		}
		
		public static string ErrorFileInvalid
		{
			get
			{
				return "Invalid File: {0}";
			}
		}
		
		public static string ErrorFileNotFound
		{
			get
			{
				return "Error: file not found: {0}";
			}
		}
		
		public static string ErrorInterfaceMissing
		{
			get
			{
				return "The INTERFACE parameter is missing";
			}
		}
		
		public static string ErrorInterfaceMultiple
		{
			get
			{
				return "A maximum of two INTERFACE parameters is valid (the second identifies the default TinyBooter port)";
			}
		}
		
		public static string ErrorInvalidDevice
		{
			get
			{
				return "The device selected is invalid";
			}
		}
		
		public static string ErrorInvalidX
		{
			get
			{
				return "Invalid {0}";
			}
		}
		
		public static string ErrorKeyFileInvalid
		{
			get
			{
				return "Invalid Key File: {0}";
			}
		}
		
		public static string ErrorKeyRequired
		{
			get
			{
				return "A private key is required for update";
			}
		}
		
		public static string ErrorNoDeploymentAssemblies
		{
			get
			{
				return "There were no valid assemblies detected in the deployment area.";
			}
		}
		
		public static string ErrorNotSupported
		{
			get
			{
				return "Not Supported";
			}
		}
		
		public static string ErrorPrefix
		{
			get
			{
				return "Error: ";
			}
		}
		
		public static string ErrorSignatureFileInvalid
		{
			get
			{
				return "The Signature File {0} is invalid";
			}
		}
		
		public static string ErrorTitleImageFile
		{
			get
			{
				return "Invalid Image File";
			}
		}
		
		public static string ErrorUnableToInstallPlugIn
		{
			get
			{
				return "Unable to install plug-in {0}";
			}
		}
		
		public static string ErrorUnableToUpdateKey
		{
			get
			{
				return "Unable to update key configuration";
			}
		}
		
		public static string ErrorUnsupportedConfiguration
		{
			get
			{
				return "Device has old or unsupported configuration";
			}
		}
		
		public static string ErrorX
		{
			get
			{
				return "Error: {0}";
			}
		}
		
		public static string Exception
		{
			get
			{
				return "Exception: {0}";
			}
		}
		
		public static string FileDialogFilterDeploymentFiles
		{
			get
			{
				return "Deployment Files (*.hex)|*.hex|All Files (*.*)|*.*||";
			}
		}
		
		public static string FileDialogFilterKeys
		{
			get
			{
				return "Key Files (*.key)|*.key|All Files (*.*)|*.*||";
			}
		}
		
		public static string HelpBanner
		{
			get
			{
				return @"**************************** MFDeploy Help (v1.0) ****************************
					Copyright (c) 2007 Microsoft Corporation All rights reserved.";
			}
		}
		
		public static string HelpCommand
		{
			get
			{
				return "Where COMMAND is:";
			}
		}
		
		public static string HelpCommandErase
		{
			get
			{
				return "- Erases deployment sectors.";
			}
		}
		
		public static string HelpCommandFlash
		{
			get
			{
				return "- Deploys the given SREC (.hex) files to the device.";
			}
		}
		
		public static string HelpCommandHelp
		{
			get
			{
				return "- Displays this screen.";
			}
		}
		
		public static string HelpCommandList
		{
			get
			{
				return "- Lists available ports (USB and Serial).";
			}
		}	
		
		public static string HelpCommandPing
		{
			get
			{
				return "- Verifies connection with device.";
			}
		}
		
		public static string HelpCommandReboot
		{
			get
			{
				return "- Reboots the device.  Warm option only restarts the CLR.";
			}
		}
		
		public static string HelpDescription
		{
			get
			{
				return @"Description: The .NET Micro Framework MFDeploy tool will be used in conjunction 
             with the Visual Studio integration to provide users the ability to 
             re-deploy firmware images and erase the deployment sector.  ";
			}
		}
		
		public static string HelpInterface
		{
			get
			{
				return "- Identifies TCP/IP address for device.";
			}
		}
		
		public static string HelpInterfaceCom
		{
			get
			{
				return "- Identifies serial port number for the device.";
			}
		}
		
		public static string HelpInterfaceTcpIp
		{
			get
			{
				return "- Identifies TCP/IP address for device.";
			}
		}
		
		public static string HelpInterfaceUsb
		{
			get
			{
				return "- Identifies USB port name for device.";
			}
		}
		
		public static string HelpInterfaceSpecial
		{
			get
			{
				return "NOTE!!! If two interfaces are given the second is assumed to be for TinyBooter";
			}
		}
		
		public static string HelpUsage
		{
			get
			{
				return @"Usage: MFDeploy [COMMAND[:CMD_ARGS]] [/I:INTERFACE[:INTEFACE_ARGS]] [FLAG]
";
			}
		}
		
		public static string IpAddress
		{
			get
			{
				return "IP Address";
			}
		}
		
		public static string KeyIndexDeployment
		{
			get
			{
				return "Deployment Sector Key";
			}
		}
		
		public static string KeyIndexFirmware
		{
			get
			{
				return "Firmware Key";
			}
		}
		
		public static string LabelDeploymentFile
		{
			get
			{
				return "Deployment File:";
			}
		}
		
		public static string LabelNewKey
		{
			get
			{
				return "New Key:";
			}
		}
		
		public static string LabelOldPrivateKey
		{
			get
			{
				return "Old Key:";
			}
		}
		
		public static string LabelPrivateKey
		{
			get
			{
				return "Key:";
			}
		}
		
		public static string MacAddress
		{
			get
			{
				return "MAC Address";
			}
		}
		
		public static string MenuItemCancel
		{
			get
			{
				return "Cancel";
			}
		}
		
		public static string MenuItemCreateApplicationDeployment
		{
			get
			{
				return "Create Application Deployment";
			}
		}
		
		public static string MenuItemPublicKeyConfiguration
		{
			get
			{
				return "Public Key Configuration";
			}
		}
		
		public static string MenuItemSignHexFile
		{
			get
			{
				return "Sign Deployment File";
			}
		}
		
		public static string MessageEraseConfirmation
		{
			get
			{
				return "Are you sure you want to erase your managed device application and data?";
			}
		}
		
		public static string MFHelpError
		{
			get
			{
				return "Cannot open help file";
			}
		}
		
		public static string MFHelpFilename
		{
			get
			{
				return "MFDeploy.chm";
			}
		}
		
		public static string NetworkTitle
		{
			get
			{
				return "Network Configuration";
			}
		}
		
		public static string OpenFileDialogFilterSREC
		{
			get
			{
				return "SREC files (*.hex)|ER_FLASH;ER_RAM;ER_CONFIG;ER_DAT;ER_ResetVector;*.hex";
			}
		}
		
		public static string PrimaryDnsAddress
		{
			get
			{
				return "Primary DNS Address";
			}
		}
		
		public static string ResultFailure
		{
			get
			{
				return "Failure";
			}
		}
		
		public static string ResultSuccess
		{
			get
			{
				return "Success";
			}
		}
		
		public static string SaveKeyTitle
		{
			get
			{
				return "Save Key As";
			}
		}
		
		public static string SecondaryDnsAddress
		{
			get
			{
				return "Secondary DNS Address";
			}
		}
		
		public static string Serial
		{
			get
			{
				return "Serial";
			}
		}
		
		public static string StatusErasing
		{
			get
			{
				return "Erasing Deployment... ";
			}
		}
		
		public static string StatusExecuting
		{
			get
			{
				return "Executing at address 0x{0:x08}";
			}
		}
		
		public static string StatusFlashing
		{
			get
			{
				return "Deploying {0}...";
			}
		}
		
		public static string StatusPinging
		{
			get
			{
				return "Pinging... ";
			}
		}
		
		public static string StatusRebooting
		{
			get
			{
				return "Rebooting...";
			}
		}
		
		public static string StatusXComplete
		{
			get
			{
				return "{0} Complete";
			}
		}
		
		public static string SubnetMask
		{
			get
			{
				return "&amp;Subnet Mask:";
			}
		}
	
		public static string TitleAppDeploy
		{
			get
			{
				return "Application Deployment";
			}
		}
		
		public static string TitleEraseConfirmation
		{
			get
			{
				return "Confirm Application Erase";
			}
		}
		
		public static string TitleErrorInput
		{
			get
			{
				return "Input Error!";
			}
		}
		
		public static string ToolStripMenuItemImageFileList
		{
			get
			{
				return "Clear Image File List";
			}
		}
		
		public static string ToolStripMenuOptions
		{
			get
			{
				return "Options";
			}
		}
		
		public static string ToolStripMenuPlugIn
		{
			get
			{
				return "Plug-in";
			}
		}
		
		public static string TransportSerial
		{
			get
			{
				return "Serial";
			}
		}
		
		public static string UsbTitle
		{
			get
			{
				return "Usb Configuration";
			}
		}
		
		public static string UserCanceled
		{
			get
			{
				return "User Cancel...";
			}
		}
		
		public static string Warning
		{
			get
			{
				return "Warning";
			}
		}
		
		public static string WarningNoFilesForDeploy
		{
			get
			{
				return "There are no files selected to deploy";
			}
		}
		
		public static string WarningPlugInPending
		{
			get
			{
				return "Plug-in command pending, please wait";
			}
		}
		
		public static string XCommand
		{
			get
			{
				return "{0} Command";
			}
		}
		
	}
}