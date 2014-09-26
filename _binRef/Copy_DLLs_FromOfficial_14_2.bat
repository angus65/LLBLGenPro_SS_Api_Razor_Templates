Robocopy "R:\AutomatedVersionRepository\_latest\PVM App Services Layer_14.2\Debug\\" %cd%   "PV.App.Managers.Standard.dll" "PV.AppService.PVM.dll" "PV.Data.Standard.DBSpecific.dll" "PV.Data.Standard.dll" "PV.Globals.dll" "SD.LLBLGen.Pro.DQE.SqlServer.dll" "SD.LLBLGen.Pro.ORMSupportClasses.dll" /V /XO
Robocopy "R:\bin_ref\pvm\pv_static\\" %cd% "NLog.dll" "PV.Logging.dll" /V /XO
Robocopy "R:\AutomatedVersionRepository\_latest\PVM Core_14.2\Debug\\" %cd% "App_Code.dll"  /V /XO

 