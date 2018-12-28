using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateWin10
{
    class TweakStore
    {
        static public bool InitTweaks(List<Category> Categorys)
        {
            /*  
             *  #########################################
             *       Telemetry & Error reporting
             *  #########################################
             */

            Category telemetryCat = new Category("Telemetry & Error Reporting");
            Categorys.Add(telemetryCat);


            // *** Telemetry ***

            Group telemetry = new Group("Disable Telemetry");
            telemetryCat.Add(telemetry);
            telemetry.Add(new Tweak("Minimize Telemetry", TweakType.SetGPO, WinVer.Win10EE)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                Name = "AllowTelemetry",
                Value = 0
            });
            telemetry.Add(new Tweak("Disable App. Compat. Telemetry", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppCompat",
                Name = "AITEnable",
                Value = 0
            });
            telemetry.Add(new Tweak("Do not Show Feedback Notifications", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                Name = "DoNotShowFeedbackNotifications",
                Value = 1
            });
            telemetry.Add(new Tweak("Disable Telemetry Service", TweakType.DisableService, WinVer.Win7) // Microsoft forced telemetry on windows 7 in a cumulative update
            {
                Name = "DiagTrack"
            });
            telemetry.Add(new Tweak("Disable Push Service", TweakType.DisableService, WinVer.Win10)
            {
                Name = "dmwappushservice"
            });
            telemetry.Add(new Tweak("Don't AllowDeviceNameInTelemetry", TweakType.SetGPO, WinVer.Win1803)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                Name = "AllowDeviceNameInTelemetry",
                Value = 0,
                Optional = true
            });
            //Disable file Diagtrack-Listener.etl
            //(Disable file diagtrack.dll)
            //(Disable file BthTelemetry.dll)


            // *** AppCompat ***
            Group appExp = new Group("Disable Application Expirience");
            telemetryCat.Add(appExp);
            appExp.Add(new Tweak("Disable Application Expirience Tasks", TweakType.DisableTask, WinVer.Win6) // or 7?
            {
                Path = @"\Microsoft\Windows\Application Experience",
                Name = "*"
            });
            appExp.Add(new Tweak("Disable Application Expirience Service", TweakType.DisableService, WinVer.Win7)
            {
                Name = "AeLookupSvc"
            });
            /*appExp.Add(new Tweak("Disable Application Information Service", TweakType.DisableService, WinVer.Win7) // this breaks UAC
            {
                Name = "Appinfo"
            });*/
            appExp.Add(new Tweak("Turn off Steps Recorder", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\windows\AppCompat",
                Name = "DisableUAR",
                Value = 1
            });
            appExp.Add(new Tweak("Turn off Inventory Collector", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\windows\AppCompat",
                Name = "DisableInventory",
                Value = 1
            });
            appExp.Add(new Tweak("Disable CompatTelRunner.exe", TweakType.BlockFile, WinVer.Win6) // or 7?
            {
                Path = @"%SystemRoot%\System32\CompatTelRunner.exe"
            });
            appExp.Add(new Tweak("Disable DiagTrackRunner.exe", TweakType.BlockFile, WinVer.Win7)
            {
                Path = @"%SystemRoot%\CompatTel\diagtrackrunner.exe"
            });
            // Disable file usbceip.dll

            Group ceip = new Group("Disable CEIP");
            telemetryCat.Add(ceip);
            ceip.Add(new Tweak("Turn of CEIP", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"SOFTWARE\Policies\Microsoft\SQMClient\Windows",
                Name = "CEIPEnable",
                Value = 0
            });
            ceip.Add(new Tweak("Disable all CEIP Tasks", TweakType.DisableTask, WinVer.Win6) // or 7?
            {
                Path = @"\Microsoft\Windows\Customer Experience Improvement Program",
                Name = "*"
            });
            ceip.Add(new Tweak("Disable IE CEIP", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Internet Explorer\SQM",
                Name = "DisableCustomerImprovementProgram",
                Value = 1,
                Optional = true
            });
            ceip.Add(new Tweak("Disable Live Messager CEIP", TweakType.SetGPO, WinVer.WinXPto7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Messenger\Client",
                Name = "CEIP",
                Value = 2,
                Optional = true
            });
            //  Set USER GPO "SOFTWARE\\Policies\\Microsoft\\Messenger\\Client" "CEIP" "2"(deprecated)


            // *** Error Repoering ***

            Group werr = new Group("Disable Error Reporting");
            telemetryCat.Add(werr);
            werr.Add(new Tweak("Turn off Windows Error Reporting", TweakType.SetRegistry, WinVer.Win6)
            {
                Path = @"SOFTWARE\Microsoft\Windows\Windows Error Reporting",
                Name = "Disabled",
                Value = 1
            });
            werr.Add(new Tweak("Turn off Windows Error Reporting (GPO)", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting",
                Name = "Disabled",
                Value = 1
            });
            werr.Add(new Tweak("Do not Report Errors", TweakType.SetGPO, WinVer.WinXPonly)
            {
                Path = @"Software\Policies\Microsoft\PCHealth\ErrorReporting",
                Name = "DoReport",
                Value = 0
            });
            werr.Add(new Tweak("Disable Error Reporting Service", TweakType.DisableService, WinVer.Win6)
            {
                Name = "WerSvc"
            });
            werr.Add(new Tweak("Disable Error Reporting Tasks", TweakType.DisableTask, WinVer.Win6)
            {
                Path = @"\Microsoft\Windows\Windows Error Reporting",
                Name = "*"
            });
            //(Disable file WerSvc.dll)
            //(Disable filea wer*.exe)


            // *** Other Diagnostics ***

            Group diag = new Group("Other Diagnostics");
            telemetryCat.Add(diag);
            diag.Add(new Tweak("Turn of MSDT", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\ScriptedDiagnosticsProvider\Policy",
                Name = "DisableQueryRemoteServer",
                Value = 1
            });
            diag.Add(new Tweak("Turn off Online Assist", TweakType.SetGPO, WinVer.Win6to7)
            {
                Path = @"Software\Policies\Microsoft\Assistance\Client\1.0",
                Name = "NoOnlineAssist",
                Value = 1
            });
            // Set USER GPO @"Software\Policies\Microsoft\Assistance\Client\1.0" "NoOnlineAssist" "1" (deprecated)
            // Set USER GPO @"Software\Policies\Microsoft\Assistance\Client\1.0" "NoExplicitFeedback" "1" (deprecated)
            // Set USER GPO @"Software\Policies\Microsoft\Assistance\Client\1.0" "NoImplicitFeedback" "1" (deprecated)
            diag.Add(new Tweak("Disable diagnosticshub Service", TweakType.DisableService, WinVer.Win10)
            {
                Name = "diagnosticshub.standardcollector.service"
            });
            diag.Add(new Tweak("Do not Update Disk Health Model", TweakType.SetGPO, WinVer.Win1709)
            {
                Path = @"Software\Policies\Microsoft\Windows\StorageHealth",
                Name = "AllowDiskHealthModelUpdates",
                Value = 0
            });
            diag.Add(new Tweak("Disable Disk Diagnostics Task", TweakType.DisableTask, WinVer.Win6) // or 7?
            {
                Path = @"\Microsoft\Windows\DiskDiagnostic",
                Name = "Microsoft-Windows-DiskDiagnosticDataCollector",
                Optional = true
            });



            /*  
             *  #########################################
             *              Cortana & search
             *  #########################################
             */

            Category searchCat = new Category("Search & Cortana");
            Categorys.Add(searchCat);


            // *** Disabel Cortana ***

            Group cortana = new Group("Disabel Cortana");
            searchCat.Add(cortana);
            cortana.Add(new Tweak("Disabel Cortana", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                Name = "AllowCortana",
                Value = 0
            });
            cortana.Add(new Tweak("Forbid the Use of Location", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                Name = "AllowSearchToUseLocation",
                Value = 0
            });


            // *** Disabel Web Search ***

            Group webSearch = new Group("Disabel Online Search");
            searchCat.Add(webSearch);
            webSearch.Add(new Tweak("Disabel Web Search", TweakType.SetGPO, WinVer.WinXP)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                Name = "DisableWebSearch",
                Value = 1
            });
            webSearch.Add(new Tweak("Disable Connected Web Search", TweakType.SetGPO, WinVer.Win81)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                Name = "ConnectedSearchUseWeb",
                Value = 0
            });
            webSearch.Add(new Tweak("Enforce Search Privacy", TweakType.SetGPO, WinVer.Win81only)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                Name = "ConnectedSearchPrivacy",
                Value = 3
            });
            // Block in firewall: searchUI.exe 


            // *** Disabel Search ***

            Group search = new Group("Disabel Search");
            searchCat.Add(search);
            search.Add(new Tweak("Disable Windows Search Service", TweakType.DisableService, WinVer.Win6)
            {
                Name = "WSearch"
            });
            search.Add(new Tweak("Disable searchUI.exe", TweakType.BlockFile, WinVer.Win10)
            {
                Path = @"c:\windows\SystemApps\Microsoft.Windows.Cortana_cw5n1h2txyewy\SearchUI.exe"
            });
            search.Add(new Tweak("Remove Search Box", TweakType.SetGPO, WinVer.Win10)
            {
                usrLevel = true,
                Path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Search",
                Name = "SearchboxTaskbarMode",
                Value = 0
            });
            webSearch.Add(new Tweak("Don't Update Search Companion", TweakType.SetGPO, WinVer.WinXPto7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\SearchCompanion",
                Name = "DisableContentFileUpdates",
                Value = 1
            });


            /*  
            *  #########################################
            *              Windows Deffender
            *  #########################################
            */

            Category defenderCat = new Category("Windows Deffender");
            Categorys.Add(defenderCat);


            // *** Disabel Deffender ***

            Group defender = new Group("Disabel Deffender");
            defenderCat.Add(defender);
            defender.Add(new Tweak("Turn off Windows Deffender", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows Defender",
                Name = "DisableAntiSpyware",
                Value = 1
            });
            defender.Add(new Tweak("Torn off Application Guard", TweakType.SetGPO, WinVer.Win10EE)
            {
                Path = @"Software\Policies\Microsoft\AppHVSI",
                Name = "AllowAppHVSI_ProviderSet",
                Value = 0
            });
            defender.Add(new Tweak("Disable SecurityHealthService Service", TweakType.SetRegistry, WinVer.Win1703)
            {
                Path= @"SYSTEM\CurrentControlSet\Services\SecurityHealthService",
                Name = "Start",
                Value = 4
            });


            // *** Silence Deffender ***

            Group defender2 = new Group("Silence Deffender");
            defenderCat.Add(defender2);
            defender2.Add(new Tweak("Disable Enhanced Notifications", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"Software\Policies\Microsoft\Windows Defender\Reporting",
                Name = "DisableEnhancedNotifications",
                Value = 1
            });
            defender2.Add(new Tweak("Disable Spynet Reporting", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"Software\Policies\Microsoft\Windows Defender\Spynet",
                Name = "SpynetReporting",
                Value = 0
            });
            defender2.Add(new Tweak("Don't Submit Samples", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"Software\Policies\Microsoft\Windows Defender\Spynet",
                Name = "SubmitSamplesConsent",
                Value = 2
            });
            //Set GPO @"SOFTWARE\Policies\Microsoft\Windows Defender\Signature Updates" "DefinitionUpdateFileSharesSources" DELET (nope)
            //Set GPO @"SOFTWARE\Policies\Microsoft\Windows Defender\Signature Updates" "FallbackOrder" "SZ:FileShares"(nope)


            // *** Disable SmartScreen ***

            Group screen = new Group("Disable SmartScreen");
            defenderCat.Add(screen);
            screen.Add(new Tweak("Turn off SmartScreen", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"Software\Policies\Microsoft\Windows\System",
                Name = "EnableSmartScreen",
                Value = 0
            });
            // Set GPO @"Software\Policies\Microsoft\Windows\System" "ShellSmartScreenLevel" DEL
            screen.Add(new Tweak("Disable App Install Control", TweakType.SetGPO, WinVer.Win1703)
            {
                Path = @"Software\Policies\Microsoft\Windows Defender\SmartScreen",
                Name = "ConfigureAppInstallControlEnabled",
                Value = 0
            });
            screen.Add(new Tweak("Disable smartscreen.exe", TweakType.BlockFile, WinVer.Win6)
            {
                Path = @"%SystemRoot%\System32\smartscreen.exe"
            });
            // Set GPO @"Software\Policies\Microsoft\Windows Defender\SmartScreen" "ConfigureAppInstallControl" DEL
            screen.Add(new Tweak("No SmartScreen for Store Apps", TweakType.SetGPO, WinVer.Win81)  // or 8
            {
                Path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost",
                Name = "EnableWebContentEvaluation",
                Value = 0
            });
            // Edge
            screen.Add(new Tweak("Disable SmartScrean for Edge", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"Software\Policies\Microsoft\MicrosoftEdge\PhishingFilter",
                Name = "EnabledV9",
                Value = 0,
                Optional = true
            });
            //Set GPO @"Software\Policies\Microsoft\MicrosoftEdge\PhishingFilter" "PreventOverride" "0"
            // Edge
            screen.Add(new Tweak("Disable SmartScrean for IE", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"Software\Policies\Microsoft\Internet Explorer\PhishingFilter",
                Name = "EnabledV9",
                Value = 0,
                Optional = true
            });


            // *** MRT-Tool ***

            Group mrt = new Group("Silence MRT-Tool");
            defenderCat.Add(mrt);
            mrt.Add(new Tweak("Don't Report Infections", TweakType.SetGPO, WinVer.Win2k)
            {
                Path = @"SOFTWARE\Policies\Microsoft\MRT",
                Name = "DontReportInfectionInformation",
                Value = 1
            });



            /*  
            *  #########################################
            *          Privacy & Advertizement
            *  #########################################
            */

            Category privacyCat = new Category("Privacy & Advertizement");
            Categorys.Add(privacyCat);


            // *** Disable Advertizement ***

            Group privacy = new Group("Disable Advertizement");
            privacyCat.Add(privacy);
            privacy.Add(new Tweak("Turn off Advertising ID", TweakType.SetGPO, WinVer.Win81)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo",
                Name = "DisabledByGroupPolicy",
                Value = 1
            });
            privacy.Add(new Tweak("Turn off Consumer Experiences", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
                Name = "DisableWindowsConsumerFeatures",
                Value = 1
            });
            privacy.Add(new Tweak("Limit Tailored Experiences", TweakType.SetGPO, WinVer.Win1703)
            {
                Path = @"Software\Microsoft\Windows\CurrentVersion\Privacy",
                Name = "TailoredExperiencesWithDiagnosticDataEnabled",
                Value = 0
            });
            privacy.Add(new Tweak("Limit Tailored Experiences (user)", TweakType.SetGPO, WinVer.Win1703)
            {
                usrLevel = true,
                Path = @"Software\Policies\Microsoft\Windows\CloudContent",
                Name = "DisableTailoredExperiencesWithDiagnosticData",
                Value = 1
            });
            privacy.Add(new Tweak("Turn off Windows Spotlight", TweakType.SetGPO, WinVer.Win10)
            {
                usrLevel = true,
                Path = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
                Name = "DisableWindowsSpotlightFeatures",
                Value = 1
            });

            privacy.Add(new Tweak("Disable OnlineTips in Settings", TweakType.SetGPO, WinVer.Win1709)
            {
                Path = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                Name = "AllowOnlineTips",
                Value = 0
            });
            privacy.Add(new Tweak("Don't show Windows Tips", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
                Name = "DisableSoftLanding",
                Value = 1
            });


            // *** No Lock Screen ***

            Group lockScr = new Group("Disable Lock Screen");
            privacyCat.Add(lockScr);
            lockScr.Add(new Tweak("Don't use Lock Screen", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Personalization",
                Name = "NoLockScreen",
                Value = 1
            });
            lockScr.Add(new Tweak("Enable LockScreen Image", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Personalization",
                Name = "LockScreenOverlaysDisabled",
                Value = 1
            });
            lockScr.Add(new Tweak("Set LockScreen Image", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Personalization",
                Name = "LockScreenImage",
                Value = "C:\\windows\\web\\screen\\lockscreen.jpg"
            });


            // *** No Personalization ***

            Group spying = new Group("No Personalization");
            privacyCat.Add(spying);
            spying.Add(new Tweak("Diable input personalization", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"SOFTWARE\Policies\Microsoft\InputPersonalization",
                Name = "AllowInputPersonalization",
                Value = 0
            });
            spying.Add(new Tweak("Disable Test Collection", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"SOFTWARE\Policies\Microsoft\InputPersonalization",
                Name = "RestrictImplicitTextCollection",
                Value = 1
            });
            spying.Add(new Tweak("Disable Inc Collection", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"SOFTWARE\Policies\Microsoft\InputPersonalization",
                Name = "RestrictImplicitInkCollection",
                Value = 1
            });
            spying.Add(new Tweak("Disable Linguistic Data Collection", TweakType.SetGPO, WinVer.Win1803)
            {
                Path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\TextInput",
                Name = "AllowLinguisticDataCollection",
                Value = 0
            });
            spying.Add(new Tweak("Disable Handwriting Error Reports", TweakType.SetGPO, WinVer.Win6to7)
            {
                Path = @"Software\Policies\Microsoft\Windows\HandwritingErrorReport",
                Name = "PreventHandwritingErrorReports",
                Value = 1
            });
            spying.Add(new Tweak("Disable Handwriting Data Sharing", TweakType.SetGPO, WinVer.Win6to7)
            {
                Path = @"Software\Policies\Microsoft\Windows\TabletPC",
                Name = "PreventHandwritingDataSharing",
                Value = 1
            });
            // Set USER GPO @"Software\Policies\Microsoft\Windows\HandwritingErrorReports" "PreventHandwritingErrorReports" "1" (deprecated)
            // Set USER GPO @"Software\Policies\Microsoft\Windows\TabletPC" "PreventHandwritingDataSharing" "1" (deprecated)	


            // *** Disable Location ***

            Group location = new Group("Protect Location");
            privacyCat.Add(location);
            location.Add(new Tweak("Disable Location Provider", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors",
                Name = "DisableLocation",
                Value = 1
            });
            location.Add(new Tweak("Don't Share Lang List", TweakType.SetGPO, WinVer.Win10)
            {
                usrLevel = true,
                Path = @"Control Panel\International\User Profile",
                Name = "HttpAcceptLanguageOptOut",
                Value = 1
            });


            // *** No Registration ***

            Group privOther = new Group("No Registration");
            privacyCat.Add(privOther);
            privOther.Add(new Tweak("Disable KMS GenTicket", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform",
                Name = "NoGenTicket",
                Value = 1
            });
            privOther.Add(new Tweak("Disable Registration", TweakType.SetGPO, WinVer.WinXP)
            {
                Path = @"Software\Policies\Microsoft\Windows\Registration Wizard Control",
                Name = "NoRegistration",
                Value = 1
            });


            // *** No Push Notifications ***

            Group push = new Group("No Push Notifications");
            privacyCat.Add(push);
            push.Add(new Tweak("Disable Cloud Notification", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications",
                Name = "NoCloudApplicationNotification",
                Value = 1
            });
            push.Add(new Tweak("Disable Cloud Notification (user)", TweakType.SetGPO, WinVer.Win8)
            {
                usrLevel = true,
                Path = @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications",
                Name = "NoCloudApplicationNotification",
                Value = 1
            });


            /*  
            *  #########################################
            *          Microsoft Account
            *  #########################################
            */

            Category accountCat = new Category("Microsoft Account");
            Categorys.Add(accountCat);


            // *** Disable OneDrive ***

            Group onedrive = new Group("Disable OneDrive");
            accountCat.Add(onedrive);
            onedrive.Add(new Tweak("Disable OneDrive Usage", TweakType.SetGPO, WinVer.Win10) // WinVer.Win7
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\OneDrive",
                Name = "DisableFileSyncNGSC",
                Value = 1
            });
            onedrive.Add(new Tweak("Silence OneDrive", TweakType.SetGPO, WinVer.Win10) // WinVer.Win7
            {
                Path = @"Software\Microsoft\OneDrive",
                Name = "PreventNetworkTrafficPreUserSignIn",
                Value = 1
            });
            // Run uninstaller


            // *** No Microsoft Accounts ***

            Group account = new Group("No Microsoft Accounts");
            accountCat.Add(account);
            account.Add(new Tweak("Disable Microsoft Accounts", TweakType.SetGPO, WinVer.Win8) // or 10?
            {
                Path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                Name = "NoConnectedUser",
                Value = 3
            });
            account.Add(new Tweak("Disable MS Account Login Service", TweakType.DisableService, WinVer.Win8) // or 10?
            {
                Name = "wlidsvc",
                Optional = true
            });


            // *** No Settings Sync ***

            Group sync = new Group("No Settings Sync");
            accountCat.Add(sync);
            sync.Add(new Tweak("Disable Settings Sync", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"Software\Policies\Microsoft\Windows\SettingSync",
                Name = "DisableSettingSync",
                Value = 2
            });
            sync.Add(new Tweak("Force Disable Settings Sync", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"Software\Policies\Microsoft\Windows\SettingSync",
                Name = "DisableSettingSyncUserOverride",
                Value = 1
            });
            sync.Add(new Tweak("Disable WiFi-Sense", TweakType.SetGPO, WinVer.Win10to1709)
            {
                Path = @"SOFTWARE\Microsoft\wcmsvc\wifinetworkmanager\config",
                Name = "AutoConnectAllowedOEM",
                Value = 0
            });


            // *** No Find My Device ***

            Group find = new Group("No Find My Device");
            accountCat.Add(find);
            find.Add(new Tweak("Don't Allow FindMyDevice", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"Software\Policies\Microsoft\FindMyDevice",
                Name = "AllowFindMyDevice",
                Value = 0
            });


            // *** No Cloud Clipboard ***

            Group ccb = new Group("No Cloud Clipboard");
            accountCat.Add(ccb);
            ccb.Add(new Tweak("Disable Cloud Clipboard", TweakType.SetGPO, WinVer.Win1809)
            {
                Path = @"Software\Policies\Microsoft\Windows\System",
                Name = "AllowCrossDeviceClipboard",
                Value = 0
            });


            // *** No Cloud Messges ***

            Group msgbak = new Group("No Cloud Messges");
            accountCat.Add(msgbak);
            msgbak.Add(new Tweak("Don't Sync Messages", TweakType.SetGPO, WinVer.Win1709)
            {
                Path = @"Software\Policies\Microsoft\Windows\Messaging",
                Name = "AllowMessageSync",
                Value = 0
            });
            msgbak.Add(new Tweak("Don't Sync Messages (user)", TweakType.SetGPO, WinVer.Win1709)
            {
                usrLevel = true,
                Path = @"SOFTWARE\Microsoft\Messaging",
                Name = "CloudServiceSyncEnabled",
                Value = 0
            });


            // *** Disable Activity Feed ***

            Group feed = new Group("Disable Activity Feed");
            accountCat.Add(feed);
            feed.Add(new Tweak("Disable Activity Feed", TweakType.SetGPO, WinVer.Win1709)
            {
                Path = @"Software\\Policies\\Microsoft\\Windows\\System",
                Name = "EnableActivityFeed",
                Value = 0
            });
            feed.Add(new Tweak("Don't Upload User Activities", TweakType.SetGPO, WinVer.Win1709)
            {
                Path = @"Software\\Policies\\Microsoft\\Windows\\System",
                Name = "UploadUserActivities",
                Value = 0
            });
            feed.Add(new Tweak("Don't Publish User Activities", TweakType.SetGPO, WinVer.Win1709)
            {
                Path = @"Software\\Policies\\Microsoft\\Windows\\System",
                Name = "PublishUserActivities",
                Value = 0
            });


            // *** No Cross Device Expirience ***

            Group cdp = new Group("No Cross Device Expirience");
            accountCat.Add(cdp);
            cdp.Add(new Tweak("Disable Cross Device Expirience", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\System",
                Name = "EnableCdp",
                Value = 0
            });


            /*  
             *  #########################################
             *               Various Others
             *  #########################################
             */


            Category miscCat = new Category("Various Others");
            Categorys.Add(miscCat);


            // *** Disable Driver Update ***

            Group drv = new Group("Disable Driver Update");
            miscCat.Add(drv);
            drv.Add(new Tweak("Don't Update Drivers With", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
                Name = "ExcludeWUDriversInQualityUpdate",
                Value = 1
            });
            drv.Add(new Tweak("Don't get Device Info from Web", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Device Metadata",
                Name = "PreventDeviceMetadataFromNetwork",
                Value = 1
            });


            // *** No Explorer AutoComplete ***

            Group ac = new Group("No Explorer AutoComplete");
            miscCat.Add(ac);
            ac.Add(new Tweak("Disable Auto Suggest", TweakType.SetGPO, WinVer.Win2k)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\Explorer\AutoComplete",
                Name = "AutoSuggest",
                Value = "no"
            });


            // *** No Speech Updates ***

            Group speech = new Group("No Speech Updates");
            miscCat.Add(speech);
            speech.Add(new Tweak("Don't Update SpeechModel", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"Software\Policies\Microsoft\Speech",
                Name = "AllowSpeechModelUpdate",
                Value = 0
            });


            // *** No Font Updates ***

            Group font = new Group("No Font Updates");
            miscCat.Add(font);
            font.Add(new Tweak("Don't Update Fonts", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\System",
                Name = "EnableFontProviders",
                Value = 0
            });


            // *** No Certificat Updates ***

            Group cert = new Group("No Certificat Updates");
            miscCat.Add(cert);
            cert.Add(new Tweak("Disable Certificate Auto Update", TweakType.SetGPO, WinVer.WinXP)
            {
                Path = @"SOFTWARE\Policies\Microsoft\SystemCertificates\AuthRoot",
                Name = "DisableRootAutoUpdate",
                Value = 1
            });


            // *** Disable NtpClient ***

            Group ntp = new Group("Disable NtpClient");
            miscCat.Add(ntp);
            ntp.Add(new Tweak("Disable NTP Client", TweakType.SetGPO, WinVer.WinXP)
            {
                Path = @"SOFTWARE\Policies\Microsoft\W32time\TimeProviders\NtpClient",
                Name = "Enabled",
                Value = 0
            });


            // *** Disable Net Status ***

            Group ncsi = new Group("Disable Net Status");
            miscCat.Add(ncsi);
            ncsi.Add(new Tweak("Disable Active Probeing", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\NetworkConnectivityStatusIndicator",
                Name = "NoActiveProbe",
                Value = 1
            });


            // *** Disable Teredo IPv6 ***

            Group teredo = new Group("Disable Teredo (IPv6)");
            miscCat.Add(teredo);
            teredo.Add(new Tweak("Disable Teredo Tunneling", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\TCPIP\v6Transition",
                Name = "Teredo_State",
                Value = "Disabled"
            });


            // *** Disable Delivery Optimisations ***

            Group dodm = new Group("No Delivery Optimisations");
            miscCat.Add(dodm);
            dodm.Add(new Tweak("Disable Delivery Optimisations", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization",
                Name = "DODownloadMode",
                Value = "100"
            });


            // *** Disable Map Updates ***

            Group map = new Group("Disable Map Updates");
            miscCat.Add(map);
            map.Add(new Tweak("Turn off unsolicited Maps Downloads", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Maps",
                Name = "AllowUntriggeredNetworkTrafficOnSettingsPage",
                Value = 0
            });
            map.Add(new Tweak("Turn off Auto Maps Update", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Maps",
                Name = "AutoDownloadAndUpdateMapData",
                Value = 0
            });


            // *** No Internet OpenWith ***

            Group iopen = new Group("No Internet OpenWith");
            miscCat.Add(iopen);
            iopen.Add(new Tweak("Disable Internet OpenWith", TweakType.SetGPO, WinVer.WinXPto7)
            {
                Path = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                Name = "NoInternetOpenWith",
                Value = 1
            });
            iopen.Add(new Tweak("Disable Internet OpenWith (user)", TweakType.SetGPO, WinVer.WinXPto7)
            {
                usrLevel = true,
                Path = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                Name = "NoInternetOpenWith",
                Value = 1
            });


            // *** Lockdown Edge ***

            Group edge = new Group("Lockdown Edge");
            miscCat.Add(edge);
            edge.Add(new Tweak("Don't Update Compatyblity Lists", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"Software\Policies\Microsoft\MicrosoftEdge\BrowserEmulation",
                Name = "MSCompatibilityMode",
                Value = 0
            });
            edge.Add(new Tweak("Set Blank Stat Page", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"Software\Policies\Microsoft\MicrosoftEdge\Internet Settings",
                Name = "ProvisionedHomePages",
                Value = "<about:blank>"
            });
            edge.Add(new Tweak("Set 'DoNotTrack'", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\MicrosoftEdge\Main",
                Name = "DoNotTrack",
                Value = 1
            });
            edge.Add(new Tweak("No Password Auto Complete", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\MicrosoftEdge\Main",
                Name = "FormSuggest Passwords",
                Value = "no"
            });
            edge.Add(new Tweak("Disable First Start Page", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\MicrosoftEdge\Main",
                Name = "PreventFirstRunPage",
                Value = 1
            });
            edge.Add(new Tweak("No Form Auto Complete", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\MicrosoftEdge\Main",
                Name = "Use FormSuggest",
                Value = "no"
            });
            edge.Add(new Tweak("Disable AddressBar Sugestions", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\MicrosoftEdge\SearchScopes",
                Name = "ShowSearchSuggestionsGlobal",
                Value = 0
            });
            edge.Add(new Tweak("Disable AddressBar (drop down) Sugestions", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\MicrosoftEdge\ServiceUI",
                Name = "ShowOneBox",
                Value = 0
            });
            edge.Add(new Tweak("Keep New Tabs Empty", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\MicrosoftEdge\ServiceUI",
                Name = "AllowWebContentOnNewTabPage",
                Value = 0
            });
            edge.Add(new Tweak("Disable Books Library Updating", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"Software\Policies\Microsoft\MicrosoftEdge\BooksLibrary",
                Name = "AllowConfigurationUpdateForBooksLibrary",
                Value = 0
            });


            // *** Lockdown IE ***

            Group ie = new Group("Lockdown IE");
            miscCat.Add(ie);
            ie.Add(new Tweak("Disable Enchanced AddressBar Sugestions", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Internet Explorer",
                Name = "AllowServicePoweredQSA",
                Value = 0
            });
            ie.Add(new Tweak("Turn off Browser Geolocation", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Internet Explorer\Geolocation",
                Name = "PolicyDisableGeolocation",
                Value = 1
            });
            ie.Add(new Tweak("Turn off Site Suggestions", TweakType.SetGPO, WinVer.WinXP)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Internet Explorer\Suggested Sites",
                Name = "Enabled",
                Value = 0
            });
            ie.Add(new Tweak("Turn off FlipAhead Prediction", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Internet Explorer\FlipAhead",
                Name = "Enabled",
                Value = 0
            });
            ie.Add(new Tweak("Disable Sync of Feeds & Slices", TweakType.SetGPO, WinVer.WinXP)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Internet Explorer\Feeds",
                Name = "BackgroundSyncStatus",
                Value = 0
            });
            ie.Add(new Tweak("Disable Compatybility View", TweakType.SetGPO, WinVer.WinXP)
            {
                Path = @"Software\Policies\Microsoft\Internet Explorer\BrowserEmulation",
                Name = "DisableSiteListEditing",
                Value = 1
            });

            ie.Add(new Tweak("Disable ActiveX Black List", TweakType.SetGPO, WinVer.WinXP)
            {
                usrLevel = true,
                Path = @"Software\Policies\Microsoft\Internet Explorer\BrowserEmulation",
                Name = "DisableSiteListEditing",
                Value = 0,
                Optional = false
            });
            ie.Add(new Tweak("Disable First Run Wizard", TweakType.SetGPO, WinVer.WinXP)
            {
                usrLevel = true,
                Path = @"Software\Policies\Microsoft\Internet Explorer\Main",
                Name = "DisableFirstRunCustomize",
                Value = 1
            });
            ie.Add(new Tweak("Set Blank Stat Page", TweakType.SetGPO, WinVer.Win2k)
            {
                usrLevel = true,
                Path = @"Software\Policies\Microsoft\Internet Explorer\Main",
                Name = "Start Page",
                Value = "about:blank"
            });
            ie.Add(new Tweak("Keep New Tabs Empty", TweakType.SetGPO, WinVer.WinXP)
            {
                usrLevel = true,
                Path = @"Software\Policies\Microsoft\Internet Explorer\TabbedBrowsing",
                Name = "NewTabPageShow",
                Value = 0
            });
 


            /*  
             *  #########################################
             *              Apps & Store
             *  #########################################
             */

            Category appCat = new Category("Apps & Store");
            Categorys.Add(appCat);


            // *** Disable Store ***

            Group store = new Group("Disable Store");
            appCat.Add(store);
            store.Add(new Tweak("Disable Store Apps", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\WindowsStore",
                Name = "DisableStoreApps",
                Value = 1
            });
            store.Add(new Tweak("Don't Auto Update Apps", TweakType.SetGPO, WinVer.Win10EE)
            {
                Path = @"SOFTWARE\Policies\Microsoft\WindowsStore",
                Name = "AutoDownload",
                Value = 2
            });
            store.Add(new Tweak("Disable App Uri Handlers", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"Software\Policies\Microsoft\Windows\System",
                Name = "EnableAppUriHandlers",
                Value = 0,
                Optional = true
            });


            // *** Lockdown Apps ***

            Group apps = new Group("Lockdown Apps");
            appCat.Add(apps);
            apps.Add(new Tweak("Don't Let Apps Access AccountInfo", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Name = "LetAppsAccessAccountInfo",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Calendar", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Name = "LetAppsAccessCalendar",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access CallHistory", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Name = "LetAppsAccessCallHistory",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Camera", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Name = "LetAppsAccessCamera",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Contacts", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Name = "LetAppsAccessContacts",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Email", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Name = "LetAppsAccessEmail",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Location", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Name = "LetAppsAccessLocation",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Messaging", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Name = "LetAppsAccessMessaging",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Microphone", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Name = "LetAppsAccessMicrophone",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Motion", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Name = "LetAppsAccessMotion",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Notifications", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Name = "LetAppsAccessNotifications",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Radios", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Name = "LetAppsAccessRadios",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Tasks", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Name = "LetAppsAccessTasks",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access TrustedDevices", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Name = "LetAppsAccessTrustedDevices",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps get Diagnostic Info", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Name = "LetAppsGetDiagnosticInfo",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Run In Background", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Name = "LetAppsRunInBackground",
                Value = 2,
                Optional = true
            });
            apps.Add(new Tweak("Don't Let Apps Sync With Devices", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Name = "LetAppsSyncWithDevices",
                Value = 2
            });


            // *** No Mail and People ***

            Group mail = new Group("Block Mail and People");
            appCat.Add(mail);
            mail.Add(new Tweak("Disable Mail App", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows Mail",
                Name = "ManualLaunchAllowed",
                Value = 0
            });
            mail.Add(new Tweak("Hide People from Taskbar", TweakType.SetGPO, WinVer.Win10)
            {
                usrLevel = true,
                Path = @"Software\Policies\Microsoft\Windows\Explorer",
                Name = "HidePeopleBar",
                Value = 1
            });

            return true;
        }
    }
}
