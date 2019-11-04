using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PrivateWin10.TweakManager;

namespace PrivateWin10
{
    public class TweakStore
    {
        [Serializable()]
        public class Category
        {
            public string Name = "";

            public Dictionary<string, Group> Groups = new Dictionary<string, Group>();

            public Category(string name)
            {
                Name = name;
            }

            public bool IsAvailable()
            {
                foreach (Group group in Groups.Values)
                {
                    if (group.IsAvailable())
                        return true;
                }
                return false;
            }

            public void Add(Tweak tweak)
            {
                Group groupe;
                if (!Groups.TryGetValue(tweak.Group, out groupe))
                {
                    groupe = new Group(tweak.Group);
                    Add(groupe);
                }

                groupe.Add(tweak);
            }

            public void Add(Group group)
            {
                if (Groups.ContainsKey(group.Name))
                    Groups.Remove(group.Name);
                group.Category = Name;
                Groups.Add(group.Name, group);
            }
        }

        [Serializable()]
        public class Group
        {
            public string Category = "";
            public string Name = "";

            public bool Recommended = false;

            public Dictionary<string, Tweak> Tweaks = new Dictionary<string, Tweak>();

            //[field: NonSerialized]
            //public event EventHandler<EventArgs> StatusChanged;

            public Group(string name, bool recommended = false)
            {
                Name = name;
                Recommended = recommended;
            }

            public bool IsAvailable()
            {
                foreach (Tweak tweak in Tweaks.Values)
                {
                    if (tweak.IsAvailable())
                        return true;
                }
                return false;
            }

            public void Add(Tweak tweak)
            {
                //tweak.StatusChanged += OnStatusChanged;

                if (Tweaks.ContainsKey(tweak.Name)) // loaded items have proproty
                    Tweaks.Remove(tweak.Name);
                if (Recommended && tweak.Hint == Tweak.Hints.None) // do not un set optional !!!
                    tweak.Hint = Tweak.Hints.Recommended;
                tweak.Category = Category;
                tweak.Group = Name;
                Tweaks.Add(tweak.Name, tweak);
            }

            //void OnStatusChanged(object sender, EventArgs arg)
            //{
            //    StatusChanged?.Invoke(this, new EventArgs());
            //}
        }

        static public bool InitTweaks(Dictionary<string, Category> Categorys)
        {
            /*  Structore:
             *      Category
             *          Groupe
             *              Tweak
             *              ...
             *          Group
             *              ...
             *      Category
             *          ...
             *      ...
             */

            /*  
             *  #########################################
             *       Telemetry & Error reporting
             *  #########################################
             */

            Category telemetryCat = new Category("Telemetry & Error Reporting"); //, "windows_telemetry");
            Categorys.Add(telemetryCat.Name, telemetryCat);


            // *** Telemetry ***

            Group telemetry = new Group("Disable Telemetry", true);
            telemetryCat.Add(telemetry);
            telemetry.Add(new Tweak("Minimize Telemetry", TweakType.SetGPO, WinVer.Win10EE)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                Key = "AllowTelemetry",
                Value = 0
            });
            telemetry.Add(new Tweak("Disable App. Compat. Telemetry", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppCompat",
                Key = "AITEnable",
                Value = 0
            });
            telemetry.Add(new Tweak("Do not Show Feedback Notifications", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                Key = "DoNotShowFeedbackNotifications",
                Value = 1
            });
            telemetry.Add(new Tweak("Disable Telemetry Service (DiagTrack-Listener)", TweakType.DisableService, WinVer.Win7) // Microsoft forced telemetry on windows 7 in a cumulative update
            {
                Key = "DiagTrack"
            });
            telemetry.Add(new Tweak("Disable AutoLogger-Diagtrack-Listener", TweakType.SetRegistry, WinVer.Win10)
            {
                Path = @"SYSTEM\CurrentControlSet\Control\WMI\Autologger\AutoLogger-Diagtrack-Listener",
                Key = "Start",
                Value = 0
            });
            telemetry.Add(new Tweak("Disable Push Service", TweakType.DisableService, WinVer.Win10)
            {
                Key = "dmwappushservice"
            });
            telemetry.Add(new Tweak("Don't AllowDeviceNameInTelemetry", TweakType.SetGPO, WinVer.Win1803)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                Key = "AllowDeviceNameInTelemetry",
                Value = 0,
                Hint = Tweak.Hints.Optional
            });
            //Disable file Diagtrack-Listener.etl
            //(Disable file diagtrack.dll)
            //(Disable file BthTelemetry.dll)


            // *** AppCompat ***
            Group appExp = new Group("Disable Application Expirience", true);
            telemetryCat.Add(appExp);
            appExp.Add(new Tweak("Disable Application Expirience Tasks", TweakType.DisableTask, WinVer.Win6) // or 7?
            {
                Path = @"\Microsoft\Windows\Application Experience",
                Key = "*"
            });
            appExp.Add(new Tweak("Disable Application Expirience Service", TweakType.DisableService, WinVer.Win7to81)
            {
                Key = "AeLookupSvc"
            });
            /*appExp.Add(new Tweak("Disable Application Information Service", TweakType.DisableService, WinVer.Win7) // this breaks UAC
            {
                Name = "Appinfo"
            });*/
            appExp.Add(new Tweak("Turn off Steps Recorder", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\windows\AppCompat",
                Key = "DisableUAR",
                Value = 1
            });
            appExp.Add(new Tweak("Turn off Inventory Collector", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\windows\AppCompat",
                Key = "DisableInventory",
                Value = 1
            });
            appExp.Add(new Tweak("Disable CompatTelRunner.exe", TweakType.BlockFile, WinVer.Win6) // or 7?
            {
                Path = @"%SystemRoot%\System32\CompatTelRunner.exe"
            });
            appExp.Add(new Tweak("Disable DiagTrackRunner.exe", TweakType.BlockFile, WinVer.Win7to81)
            {
                Path = @"%SystemRoot%\CompatTel\diagtrackrunner.exe"
            });
            // Disable file usbceip.dll

            Group ceip = new Group("Disable CEIP", true);
            telemetryCat.Add(ceip);
            ceip.Add(new Tweak("Turn of CEIP", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"SOFTWARE\Policies\Microsoft\SQMClient\Windows",
                Key = "CEIPEnable",
                Value = 0
            });
            ceip.Add(new Tweak("Disable all CEIP Tasks", TweakType.DisableTask, WinVer.Win6) // or 7?
            {
                Path = @"\Microsoft\Windows\Customer Experience Improvement Program",
                Key = "*"
            });
            ceip.Add(new Tweak("Disable IE CEIP", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Internet Explorer\SQM",
                Key = "DisableCustomerImprovementProgram",
                Value = 1,
                Hint = Tweak.Hints.Optional
            });
            ceip.Add(new Tweak("Disable Live Messager CEIP", TweakType.SetGPO, WinVer.WinXPto7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Messenger\Client",
                Key = "CEIP",
                Value = 2,
                Hint = Tweak.Hints.Optional
            });
            //  Set USER GPO "SOFTWARE\\Policies\\Microsoft\\Messenger\\Client" "CEIP" "2"(deprecated)


            // *** Error Repoering ***

            Group werr = new Group("Disable Error Reporting", true);
            telemetryCat.Add(werr);
            werr.Add(new Tweak("Turn off Windows Error Reporting", TweakType.SetRegistry, WinVer.Win6)
            {
                Path = @"SOFTWARE\Microsoft\Windows\Windows Error Reporting",
                Key = "Disabled",
                Value = 1
            });
            werr.Add(new Tweak("Turn off Windows Error Reporting (GPO)", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting",
                Key = "Disabled",
                Value = 1
            });
            werr.Add(new Tweak("Do not Report Errors", TweakType.SetGPO, WinVer.WinXPonly)
            {
                Path = @"Software\Policies\Microsoft\PCHealth\ErrorReporting",
                Key = "DoReport",
                Value = 0
            });
            werr.Add(new Tweak("Disable Error Reporting Service", TweakType.DisableService, WinVer.Win6)
            {
                Key = "WerSvc"
            });
            werr.Add(new Tweak("Disable Error Reporting Tasks", TweakType.DisableTask, WinVer.Win6)
            {
                Path = @"\Microsoft\Windows\Windows Error Reporting",
                Key = "*"
            });
            //(Disable file WerSvc.dll)
            //(Disable filea wer*.exe)


            // *** Other Diagnostics ***

            Group diag = new Group("Other Diagnostics");
            telemetryCat.Add(diag);
            diag.Add(new Tweak("Turn of MSDT", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\ScriptedDiagnosticsProvider\Policy",
                Key = "DisableQueryRemoteServer",
                Value = 1
            });
            diag.Add(new Tweak("Turn off Online Assist", TweakType.SetGPO, WinVer.Win6to7)
            {
                Path = @"Software\Policies\Microsoft\Assistance\Client\1.0",
                Key = "NoOnlineAssist",
                Value = 1
            });
            // Set USER GPO @"Software\Policies\Microsoft\Assistance\Client\1.0" "NoOnlineAssist" "1" (deprecated)
            // Set USER GPO @"Software\Policies\Microsoft\Assistance\Client\1.0" "NoExplicitFeedback" "1" (deprecated)
            // Set USER GPO @"Software\Policies\Microsoft\Assistance\Client\1.0" "NoImplicitFeedback" "1" (deprecated)
            diag.Add(new Tweak("Disable diagnosticshub Service", TweakType.DisableService, WinVer.Win10)
            {
                Key = "diagnosticshub.standardcollector.service"
            });
            diag.Add(new Tweak("Do not Update Disk Health Model", TweakType.SetGPO, WinVer.Win1709)
            {
                Path = @"Software\Policies\Microsoft\Windows\StorageHealth",
                Key = "AllowDiskHealthModelUpdates",
                Value = 0
            });
            diag.Add(new Tweak("Disable Disk Diagnostics Task", TweakType.DisableTask, WinVer.Win6) // or 7?
            {
                Path = @"\Microsoft\Windows\DiskDiagnostic",
                Key = "Microsoft-Windows-DiskDiagnosticDataCollector",
                Hint = Tweak.Hints.Optional
            });



            /*  
             *  #########################################
             *              Cortana & search
             *  #########################################
             */

            Category searchCat = new Category("Search & Cortana"); //, "windows_search");
            Categorys.Add(searchCat.Name, searchCat);


            // *** Disable Cortana ***

            Group cortana = new Group("Disable Cortana");
            searchCat.Add(cortana);
            cortana.Add(new Tweak("Disable Cortana", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                Key = "AllowCortana",
                Value = 0
            });
            cortana.Add(new Tweak("Forbid the Use of Location", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                Key = "AllowSearchToUseLocation",
                Value = 0
            });


            // *** Disable Web Search ***

            Group webSearch = new Group("Disable Online Search", true);
            searchCat.Add(webSearch);
            webSearch.Add(new Tweak("Disable Web Search", TweakType.SetGPO, WinVer.WinXP)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                Key = "DisableWebSearch",
                Value = 1
            });
            webSearch.Add(new Tweak("Disable Connected Web Search", TweakType.SetGPO, WinVer.Win81)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                Key = "ConnectedSearchUseWeb",
                Value = 0
            });
            webSearch.Add(new Tweak("Enforce Search Privacy", TweakType.SetGPO, WinVer.Win81only)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                Key = "ConnectedSearchPrivacy",
                Value = 3
            });
            // Block in firewall: searchUI.exe 


            // *** Disable Search ***

            Group search = new Group("Disable Search");
            searchCat.Add(search);
            search.Add(new Tweak("Disable Windows Search Service", TweakType.DisableService, WinVer.Win6)
            {
                Key = "WSearch"
            });
            search.Add(new Tweak("Disable searchUI.exe", TweakType.BlockFile, WinVer.Win10)
            {
                Path = @"c:\windows\SystemApps\Microsoft.Windows.Cortana_cw5n1h2txyewy\SearchUI.exe",
                Hint = Tweak.Hints.Optional
            });
            search.Add(new Tweak("Remove Search Box", TweakType.SetGPO, WinVer.Win10)
            {
                usrLevel = true,
                Path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Search",
                Key = "SearchboxTaskbarMode",
                Value = 0
            });
            webSearch.Add(new Tweak("Don't Update Search Companion", TweakType.SetGPO, WinVer.WinXPto7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\SearchCompanion",
                Key = "DisableContentFileUpdates",
                Value = 1
            });


            /*  
            *  #########################################
            *              Windows Defender
            *  #########################################
            */

            Category defenderCat = new Category("Windows Defender"); //, "microsoft_defender" );
            Categorys.Add(defenderCat.Name, defenderCat);


            // *** Disable Defender ***

            Group defender = new Group("Disable Defender");
            defenderCat.Add(defender);
            //defender.Add(new Tweak("Turn off Windows Defender", TweakType.SetGPO, WinVer.Win6)
            defender.Add(new Tweak("Disable Real-Time Protection", TweakType.SetGPO, WinVer.Win10) // starting with windows 1903 this must be disabled first to turn off tamper protection
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection",
                Key = "DisableRealtimeMonitoring",
                Value = 1
            });
            defender.Add(new Tweak("Turn off Anti Spyware", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows Defender",
                Key = "DisableAntiSpyware",
                Value = 1
            });
            /*defender.Add(new Tweak("Turn off Anti Virus", TweakType.SetGPO, WinVer.Win6) // legacy not neede
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows Defender",
                Name = "DisableAntiVirus",
                Value = 1
            });*/
            defender.Add(new Tweak("Torn off Application Guard", TweakType.SetGPO, WinVer.Win10EE)
            {
                Path = @"SOFTWARE\Policies\Microsoft\AppHVSI",
                Key = "AllowAppHVSI_ProviderSet",
                Value = 0
            });
            defender.Add(new Tweak("Disable SecurityHealthService Service", TweakType.SetRegistry, WinVer.Win1703)
            {
                Path= @"SYSTEM\CurrentControlSet\Services\SecurityHealthService",
                Key = "Start",
                Value = 4
            });


            // *** Silence Defender ***

            Group defender2 = new Group("Silence Defender", true);
            defenderCat.Add(defender2);
            defender2.Add(new Tweak("Disable Enhanced Notifications", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"Software\Policies\Microsoft\Windows Defender\Reporting",
                Key = "DisableEnhancedNotifications",
                Value = 1
            });
            defender2.Add(new Tweak("Disable Spynet Reporting", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"Software\Policies\Microsoft\Windows Defender\Spynet",
                Key = "SpynetReporting",
                Value = 0
            });
            defender2.Add(new Tweak("Don't Submit Samples", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"Software\Policies\Microsoft\Windows Defender\Spynet",
                Key = "SubmitSamplesConsent",
                Value = 2
            });
            //Set GPO @"SOFTWARE\Policies\Microsoft\Windows Defender\Signature Updates" "DefinitionUpdateFileSharesSources" DELET (nope)
            //Set GPO @"SOFTWARE\Policies\Microsoft\Windows Defender\Signature Updates" "FallbackOrder" "SZ:FileShares"(nope)


            // *** Disable SmartScreen ***

            Group screen = new Group("Disable SmartScreen", true);
            defenderCat.Add(screen);
            screen.Add(new Tweak("Turn off SmartScreen", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"Software\Policies\Microsoft\Windows\System",
                Key = "EnableSmartScreen",
                Value = 0
            });
            // Set GPO @"Software\Policies\Microsoft\Windows\System" "ShellSmartScreenLevel" DEL
            screen.Add(new Tweak("Disable App Install Control", TweakType.SetGPO, WinVer.Win1703)
            {
                Path = @"Software\Policies\Microsoft\Windows Defender\SmartScreen",
                Key = "ConfigureAppInstallControlEnabled",
                Value = 0
            });
            screen.Add(new Tweak("Disable smartscreen.exe", TweakType.BlockFile, WinVer.Win6)
            {
                Path = @"%SystemRoot%\System32\smartscreen.exe",
                Hint = Tweak.Hints.Optional
            });
            // Set GPO @"Software\Policies\Microsoft\Windows Defender\SmartScreen" "ConfigureAppInstallControl" DEL
            screen.Add(new Tweak("No SmartScreen for Store Apps", TweakType.SetGPO, WinVer.Win81)  // or 8
            {
                Path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost",
                Key = "EnableWebContentEvaluation",
                Value = 0
            });
            // Edge
            screen.Add(new Tweak("Disable SmartScrean for Edge", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"Software\Policies\Microsoft\MicrosoftEdge\PhishingFilter",
                Key = "EnabledV9",
                Value = 0,
                Hint = Tweak.Hints.Optional
            });
            //Set GPO @"Software\Policies\Microsoft\MicrosoftEdge\PhishingFilter" "PreventOverride" "0"
            // Edge
            screen.Add(new Tweak("Disable SmartScrean for IE", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"Software\Policies\Microsoft\Internet Explorer\PhishingFilter",
                Key = "EnabledV9",
                Value = 0,
                Hint = Tweak.Hints.Optional
            });


            // *** MRT-Tool ***

            Group mrt = new Group("Silence MRT-Tool", true);
            defenderCat.Add(mrt);
            mrt.Add(new Tweak("Don't Report Infections", TweakType.SetGPO, WinVer.Win2k)
            {
                Path = @"SOFTWARE\Policies\Microsoft\MRT",
                Key = "DontReportInfectionInformation",
                Value = 1
            });



            /*  
            *  #########################################
            *          Privacy & Advertisement
            *  #########################################
            */

            Category privacyCat = new Category("Privacy & Advertisement"); //, "windows_privacy");
            Categorys.Add(privacyCat.Name, privacyCat);


            // *** Disable Advertizement ***

            Group privacy = new Group("Disable Advertizement", true);
            privacyCat.Add(privacy);
            privacy.Add(new Tweak("Turn off Advertising ID", TweakType.SetGPO, WinVer.Win81)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo",
                Key = "DisabledByGroupPolicy",
                Value = 1
            });
            privacy.Add(new Tweak("Turn off Consumer Experiences", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
                Key = "DisableWindowsConsumerFeatures",
                Value = 1
            });
            privacy.Add(new Tweak("Limit Tailored Experiences", TweakType.SetGPO, WinVer.Win1703)
            {
                Path = @"Software\Microsoft\Windows\CurrentVersion\Privacy",
                Key = "TailoredExperiencesWithDiagnosticDataEnabled",
                Value = 0
            });
            privacy.Add(new Tweak("Limit Tailored Experiences (user)", TweakType.SetGPO, WinVer.Win1703)
            {
                usrLevel = true,
                Path = @"Software\Policies\Microsoft\Windows\CloudContent",
                Key = "DisableTailoredExperiencesWithDiagnosticData",
                Value = 1
            });
            privacy.Add(new Tweak("Turn off Windows Spotlight", TweakType.SetGPO, WinVer.Win10)
            {
                usrLevel = true,
                Path = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
                Key = "DisableWindowsSpotlightFeatures",
                Value = 1
            });

            privacy.Add(new Tweak("Disable OnlineTips in Settings", TweakType.SetGPO, WinVer.Win1709)
            {
                Path = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                Key = "AllowOnlineTips",
                Value = 0
            });
            privacy.Add(new Tweak("Don't show Windows Tips", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
                Key = "DisableSoftLanding",
                Value = 1
            });


            // *** No Lock Screen ***

            Group lockScr = new Group("Disable Lock Screen", true);
            privacyCat.Add(lockScr);
            lockScr.Add(new Tweak("Don't use Lock Screen", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Personalization",
                Key = "NoLockScreen",
                Value = 1
            });
            lockScr.Add(new Tweak("Enable LockScreen Image", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Personalization",
                Key = "LockScreenOverlaysDisabled",
                Value = 1
            });
            lockScr.Add(new Tweak("Set LockScreen Image", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Personalization",
                Key = "LockScreenImage",
                Value = "C:\\windows\\web\\screen\\lockscreen.jpg"
            });


            // *** No Personalization ***

            Group spying = new Group("No Personalization", true);
            privacyCat.Add(spying);
            spying.Add(new Tweak("Diable input personalization", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"SOFTWARE\Policies\Microsoft\InputPersonalization",
                Key = "AllowInputPersonalization",
                Value = 0
            });
            spying.Add(new Tweak("Disable Test Collection", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"SOFTWARE\Policies\Microsoft\InputPersonalization",
                Key = "RestrictImplicitTextCollection",
                Value = 1
            });
            spying.Add(new Tweak("Disable Inc Collection", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"SOFTWARE\Policies\Microsoft\InputPersonalization",
                Key = "RestrictImplicitInkCollection",
                Value = 1
            });
            spying.Add(new Tweak("Disable Linguistic Data Collection", TweakType.SetGPO, WinVer.Win1803)
            {
                Path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\TextInput",
                Key = "AllowLinguisticDataCollection",
                Value = 0
            });
            spying.Add(new Tweak("Disable Handwriting Error Reports", TweakType.SetGPO, WinVer.Win6to7)
            {
                Path = @"Software\Policies\Microsoft\Windows\HandwritingErrorReport",
                Key = "PreventHandwritingErrorReports",
                Value = 1
            });
            spying.Add(new Tweak("Disable Handwriting Data Sharing", TweakType.SetGPO, WinVer.Win6to7)
            {
                Path = @"Software\Policies\Microsoft\Windows\TabletPC",
                Key = "PreventHandwritingDataSharing",
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
                Key = "DisableLocation",
                Value = 1
            });
            location.Add(new Tweak("Don't Share Lang List", TweakType.SetGPO, WinVer.Win10)
            {
                usrLevel = true,
                Path = @"Control Panel\International\User Profile",
                Key = "HttpAcceptLanguageOptOut",
                Value = 1
            });


            // *** No Registration ***

            Group privOther = new Group("No Registration", true);
            privacyCat.Add(privOther);
            privOther.Add(new Tweak("Disable KMS GenTicket", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform",
                Key = "NoGenTicket",
                Value = 1
            });
            privOther.Add(new Tweak("Disable Registration", TweakType.SetGPO, WinVer.WinXP)
            {
                Path = @"Software\Policies\Microsoft\Windows\Registration Wizard Control",
                Key = "NoRegistration",
                Value = 1
            });


            // *** No Push Notifications ***

            Group push = new Group("No Push Notifications", true);
            privacyCat.Add(push);
            push.Add(new Tweak("Disable Cloud Notification", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications",
                Key = "NoCloudApplicationNotification",
                Value = 1
            });
            push.Add(new Tweak("Disable Cloud Notification (user)", TweakType.SetGPO, WinVer.Win8)
            {
                usrLevel = true,
                Path = @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications",
                Key = "NoCloudApplicationNotification",
                Value = 1
            });


            /*  
            *  #########################################
            *          Microsoft Account
            *  #########################################
            */

            Category accountCat = new Category("Microsoft Account"); //, "microsoft_account");
            Categorys.Add(accountCat.Name, accountCat);


            // *** Disable OneDrive ***

            Group onedrive = new Group("Disable OneDrive", true);
            accountCat.Add(onedrive);
            onedrive.Add(new Tweak("Disable OneDrive Usage", TweakType.SetGPO, WinVer.Win10) // WinVer.Win7
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\OneDrive",
                Key = "DisableFileSyncNGSC",
                Value = 1
            });
            onedrive.Add(new Tweak("Silence OneDrive", TweakType.SetGPO, WinVer.Win10) // WinVer.Win7
            {
                Path = @"Software\Microsoft\OneDrive",
                Key = "PreventNetworkTrafficPreUserSignIn",
                Value = 1
            });
            // Run uninstaller


            // *** No Microsoft Accounts ***

            Group account = new Group("No Microsoft Accounts", true);
            accountCat.Add(account);
            account.Add(new Tweak("Disable Microsoft Accounts", TweakType.SetGPO, WinVer.Win8) // or 10?
            {
                Path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                Key = "NoConnectedUser",
                Value = 3
            });
            account.Add(new Tweak("Disable MS Account Login Service", TweakType.DisableService, WinVer.Win8) // or 10?
            {
                Key = "wlidsvc",
                Hint = Tweak.Hints.Optional
            });


            // *** No Settings Sync ***

            Group sync = new Group("No Settings Sync", true);
            accountCat.Add(sync);
            sync.Add(new Tweak("Disable Settings Sync", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"Software\Policies\Microsoft\Windows\SettingSync",
                Key = "DisableSettingSync",
                Value = 2
            });
            sync.Add(new Tweak("Force Disable Settings Sync", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"Software\Policies\Microsoft\Windows\SettingSync",
                Key = "DisableSettingSyncUserOverride",
                Value = 1
            });
            sync.Add(new Tweak("Disable WiFi-Sense", TweakType.SetGPO, WinVer.Win10to1709)
            {
                Path = @"SOFTWARE\Microsoft\wcmsvc\wifinetworkmanager\config",
                Key = "AutoConnectAllowedOEM",
                Value = 0
            });


            // *** No Find My Device ***

            Group find = new Group("No Find My Device", true);
            accountCat.Add(find);
            find.Add(new Tweak("Don't Allow FindMyDevice", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"Software\Policies\Microsoft\FindMyDevice",
                Key = "AllowFindMyDevice",
                Value = 0
            });


            // *** No Cloud Clipboard ***

            Group ccb = new Group("No Cloud Clipboard", true);
            accountCat.Add(ccb);
            ccb.Add(new Tweak("Disable Cloud Clipboard", TweakType.SetGPO, WinVer.Win1809)
            {
                Path = @"Software\Policies\Microsoft\Windows\System",
                Key = "AllowCrossDeviceClipboard",
                Value = 0
            });


            // *** No Cloud Messges ***

            Group msgbak = new Group("No Cloud Messges", true);
            accountCat.Add(msgbak);
            msgbak.Add(new Tweak("Don't Sync Messages", TweakType.SetGPO, WinVer.Win1709)
            {
                Path = @"Software\Policies\Microsoft\Windows\Messaging",
                Key = "AllowMessageSync",
                Value = 0
            });
            msgbak.Add(new Tweak("Don't Sync Messages (user)", TweakType.SetGPO, WinVer.Win1709)
            {
                usrLevel = true,
                Path = @"SOFTWARE\Microsoft\Messaging",
                Key = "CloudServiceSyncEnabled",
                Value = 0
            });


            // *** Disable Activity Feed ***

            Group feed = new Group("Disable Activity Feed", true);
            accountCat.Add(feed);
            feed.Add(new Tweak("Disable Activity Feed", TweakType.SetGPO, WinVer.Win1709)
            {
                Path = @"Software\\Policies\\Microsoft\\Windows\\System",
                Key = "EnableActivityFeed",
                Value = 0
            });
            feed.Add(new Tweak("Don't Upload User Activities", TweakType.SetGPO, WinVer.Win1709)
            {
                Path = @"Software\\Policies\\Microsoft\\Windows\\System",
                Key = "UploadUserActivities",
                Value = 0
            });
            feed.Add(new Tweak("Don't Publish User Activities", TweakType.SetGPO, WinVer.Win1709)
            {
                Path = @"Software\\Policies\\Microsoft\\Windows\\System",
                Key = "PublishUserActivities",
                Value = 0
            });


            // *** No Cross Device Expirience ***

            Group cdp = new Group("No Cross Device Expirience", true);
            accountCat.Add(cdp);
            cdp.Add(new Tweak("Disable Cross Device Expirience", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\System",
                Key = "EnableCdp",
                Value = 0
            });

            /*  
            *  #########################################
            *          Microsoft Office
            *  #########################################
            */

            Category officeCat = new Category("Microsoft Office"); //, "microsoft_office");
            Categorys.Add(officeCat.Name, officeCat);


            // *** Disable Office Telemetry 0 ***

            Group officeTelemetry = new Group("Disable Telemetry Components", true);
            officeCat.Add(officeTelemetry);
            officeTelemetry.Add(new Tweak("Office Automatic Updates", TweakType.DisableTask, WinVer.Win7)
            {
                Path = @"\Microsoft\Office",
                Key = "Office Automatic Updates",
                Hint = Tweak.Hints.Optional
            });
            officeTelemetry.Add(new Tweak("Office Automatic Updates 2.0", TweakType.DisableTask, WinVer.Win7)
            {
                Path = @"\Microsoft\Office",
                Key = "Office Automatic Updates 2.0",
                Hint = Tweak.Hints.Optional
            });
            officeTelemetry.Add(new Tweak("OfficeTelemetryAgentFallBack2016&2019", TweakType.DisableTask, WinVer.Win7)
            {
                Path = @"\Microsoft\Office",
                Key = "OfficeTelemetryAgentFallBack2016"
            });
            officeTelemetry.Add(new Tweak("OfficeTelemetryAgentLogOn2016&2019", TweakType.DisableTask, WinVer.Win7)
            {
                Path = @"\Microsoft\Office",
                Key = "OfficeTelemetryAgentLogOn2016"
            });
            officeTelemetry.Add(new Tweak("Office ClickToRun Service Monitor", TweakType.DisableTask, WinVer.Win7)
            {
                Path = @"\Microsoft\Office",
                Key = "Office ClickToRun Service Monitor",
                Hint = Tweak.Hints.Optional
            });
            officeTelemetry.Add(new Tweak("Disable Office Telemetry Process", TweakType.BlockFile, WinVer.Win7)
            {
                Path = @"C:\program files\microsoft office\root\office16\msoia.exe",
                Hint = Tweak.Hints.Optional
            });

            // *** Disable Office Telemetry 1 ***

            Group officeTelemetryOSM = new Group("Disable Telemetry OSM", true);
            officeCat.Add(officeTelemetryOSM);
            officeTelemetryOSM.Add(new Tweak("Enablelogging", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\OSM",
                Key = "Enablelogging",
                Value = 0
            });
            officeTelemetryOSM.Add(new Tweak("EnableUpload", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\OSM",
                Key = "EnableUpload",
                Value = 0
            });
            officeTelemetryOSM.Add(new Tweak("EnableFileObfuscation", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\OSM",
                Key = "EnableFileObfuscation",
                Value = 1
            });

            // *** Disable Office Telemetry 2 ***

            Group officeTelemetryCommon = new Group("Disable Telemetry Common", true);
            officeCat.Add(officeTelemetryCommon);
            officeTelemetryCommon.Add(new Tweak("qmenable", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\Common",
                Key = "qmenable",
                Value = 0
            });
            officeTelemetryCommon.Add(new Tweak("sendcustomerdata", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\Common",
                Key = "sendcustomerdata",
                Value = 0
            });
            officeTelemetryCommon.Add(new Tweak("updatereliabilitydata", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\Common",
                Key = "updatereliabilitydata",
                Value = 0
            });

            // *** Disable Office Telemetry 3 ***

            Group officeTelemetryFeadback = new Group("Disable Telemetry Feadback", true);
            officeCat.Add(officeTelemetryFeadback);
            officeTelemetryFeadback.Add(new Tweak("feedback", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\Common\feedback",
                Key = "enabled",
                Value = 0
            });
            officeTelemetryFeadback.Add(new Tweak("includescreenshot", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\Common\feedback",
                Key = "includescreenshot",
                Value = 0
            });
            officeTelemetryFeadback.Add(new Tweak("ptwoptin", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\Common\ptwatson",
                Key = "ptwoptin",
                Value = 0
            });

            // *** Disable Office Telemetry 4 ***

            Group officeTelemetryByApp = new Group("Disable Telemetry by Application", true);
            officeCat.Add(officeTelemetryByApp);
            officeTelemetryByApp.Add(new Tweak("access", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\OSM\preventedapplications",
                Key = "accesssolution",
                Value = 1
            });
            officeTelemetryByApp.Add(new Tweak("olk", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\OSM\preventedapplications",
                Key = "olksolution",
                Value = 1
            });
            officeTelemetryByApp.Add(new Tweak("onenote", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\OSM\preventedapplications",
                Key = "onenotesolution",
                Value = 1
            });
            officeTelemetryByApp.Add(new Tweak("ppt", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\OSM\preventedapplications",
                Key = "pptsolution",
                Value = 1
            });
            officeTelemetryByApp.Add(new Tweak("project", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\OSM\preventedapplications",
                Key = "projectsolution",
                Value = 1
            });
            officeTelemetryByApp.Add(new Tweak("publisher", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\OSM\preventedapplications",
                Key = "publishersolution",
                Value = 1
            });
            officeTelemetryByApp.Add(new Tweak("visio", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\OSM\preventedapplications",
                Key = "visiosolution",
                Value = 1
            });
            officeTelemetryByApp.Add(new Tweak("wd", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\OSM\preventedapplications",
                Key = "wdsolution",
                Value = 1
            });
            officeTelemetryByApp.Add(new Tweak("xl", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\OSM\preventedapplications",
                Key = "xlsolution",
                Value = 1
            });

            // *** Disable Office Telemetry 5 ***

            Group officeTelemetryByType = new Group("Disable Telemetry by Type", true);
            officeCat.Add(officeTelemetryByType);
            officeTelemetryByType.Add(new Tweak("agave", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\OSM\preventedsolutiontypes",
                Key = "agave",
                Value = 1
            });
            officeTelemetryByType.Add(new Tweak("appaddins", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\OSM\preventedsolutiontypes",
                Key = "appaddins",
                Value = 1
            });
            officeTelemetryByType.Add(new Tweak("comaddins", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\OSM\preventedsolutiontypes",
                Key = "comaddins",
                Value = 1
            });
            officeTelemetryByType.Add(new Tweak("documentfiles", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\OSM\preventedsolutiontypes",
                Key = "documentfiles",
                Value = 1
            });
            officeTelemetryByType.Add(new Tweak("templatefiles", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\OSM\preventedsolutiontypes",
                Key = "templatefiles",
                Value = 1
            });


            // *** Disable Office Online Features ***

            Group officeOnline = new Group("Disable Online Features", true);
            officeCat.Add(officeOnline);
            officeOnline.Add(new Tweak("skydrivesigninoption", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\Common\General",
                Key = "skydrivesigninoption",
                Value = 0
            });
            officeOnline.Add(new Tweak("shownfirstrunoptin", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\Common\General",
                Key = "shownfirstrunoptin",
                Value = 1
            });
            officeOnline.Add(new Tweak("disablemovie", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"Software\Policies\Microsoft\Office\16.0\Firstrun",
                Key = "disablemovie",
                Value = 1
            });

            /*  
             *  #########################################
             *               Various Others
             *  #########################################
             */


            Category miscCat = new Category("Various Others"); //, "windows_misc");
            Categorys.Add(miscCat.Name, miscCat);


            // *** Disable Driver Update ***

            Group drv = new Group("Disable Driver Update");
            miscCat.Add(drv);
            drv.Add(new Tweak("Don't Update Drivers With", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
                Key = "ExcludeWUDriversInQualityUpdate",
                Value = 1
            });
            drv.Add(new Tweak("Don't get Device Info from Web", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Device Metadata",
                Key = "PreventDeviceMetadataFromNetwork",
                Value = 1
            });


            // *** No Explorer AutoComplete ***

            Group ac = new Group("No Explorer AutoComplete", true);
            miscCat.Add(ac);
            ac.Add(new Tweak("Disable Auto Suggest", TweakType.SetGPO, WinVer.Win2k)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\Explorer\AutoComplete",
                Key = "AutoSuggest",
                Value = "no"
            });


            // *** No Speech Updates ***

            Group speech = new Group("No Speech Updates");
            miscCat.Add(speech);
            speech.Add(new Tweak("Don't Update SpeechModel", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"Software\Policies\Microsoft\Speech",
                Key = "AllowSpeechModelUpdate",
                Value = 0
            });


            // *** No Font Updates ***

            Group font = new Group("No Font Updates");
            miscCat.Add(font);
            font.Add(new Tweak("Don't Update Fonts", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\System",
                Key = "EnableFontProviders",
                Value = 0
            });


            // *** No Certificat Updates ***

            Group cert = new Group("No Certificat Updates");
            miscCat.Add(cert);
            cert.Add(new Tweak("Disable Certificate Auto Update", TweakType.SetGPO, WinVer.WinXP)
            {
                Path = @"SOFTWARE\Policies\Microsoft\SystemCertificates\AuthRoot",
                Key = "DisableRootAutoUpdate",
                Value = 1
            });


            // *** Disable NtpClient ***

            Group ntp = new Group("Disable NtpClient");
            miscCat.Add(ntp);
            ntp.Add(new Tweak("Disable NTP Client", TweakType.SetGPO, WinVer.WinXP)
            {
                Path = @"SOFTWARE\Policies\Microsoft\W32time\TimeProviders\NtpClient",
                Key = "Enabled",
                Value = 0
            });


            // *** Disable Net Status ***

            Group ncsi = new Group("Disable Net Status");
            miscCat.Add(ncsi);
            ncsi.Add(new Tweak("Disable Active Probeing", TweakType.SetGPO, WinVer.Win6)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\NetworkConnectivityStatusIndicator",
                Key = "NoActiveProbe",
                Value = 1
            });


            // *** Disable Teredo IPv6 ***

            Group teredo = new Group("Disable Teredo (IPv6)");
            miscCat.Add(teredo);
            teredo.Add(new Tweak("Disable Teredo Tunneling", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\TCPIP\v6Transition",
                Key = "Teredo_State",
                Value = "Disabled"
            });


            // *** Disable Delivery Optimisations ***

            Group dodm = new Group("No Delivery Optimisations", true);
            miscCat.Add(dodm);
            dodm.Add(new Tweak("Disable Delivery Optimisations", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization",
                Key = "DODownloadMode",
                Value = "100"
            });


            // *** Disable Map Updates ***

            Group map = new Group("Disable Map Updates");
            miscCat.Add(map);
            map.Add(new Tweak("Turn off unsolicited Maps Downloads", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Maps",
                Key = "AllowUntriggeredNetworkTrafficOnSettingsPage",
                Value = 0
            });
            map.Add(new Tweak("Turn off Auto Maps Update", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\Maps",
                Key = "AutoDownloadAndUpdateMapData",
                Value = 0
            });


            // *** No Internet OpenWith ***

            Group iopen = new Group("No Internet OpenWith", true);
            miscCat.Add(iopen);
            iopen.Add(new Tweak("Disable Internet OpenWith", TweakType.SetGPO, WinVer.WinXPto7)
            {
                Path = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                Key = "NoInternetOpenWith",
                Value = 1
            });
            iopen.Add(new Tweak("Disable Internet OpenWith (user)", TweakType.SetGPO, WinVer.WinXPto7)
            {
                usrLevel = true,
                Path = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                Key = "NoInternetOpenWith",
                Value = 1
            });


            // *** Lockdown Edge ***

            Group edge = new Group("Lockdown Edge");
            miscCat.Add(edge);
            edge.Add(new Tweak("Don't Update Compatyblity Lists", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"Software\Policies\Microsoft\MicrosoftEdge\BrowserEmulation",
                Key = "MSCompatibilityMode",
                Value = 0
            });
            edge.Add(new Tweak("Set Blank Stat Page", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"Software\Policies\Microsoft\MicrosoftEdge\Internet Settings",
                Key = "ProvisionedHomePages",
                Value = "<about:blank>"
            });
            edge.Add(new Tweak("Set 'DoNotTrack'", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\MicrosoftEdge\Main",
                Key = "DoNotTrack",
                Value = 1
            });
            edge.Add(new Tweak("No Password Auto Complete", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\MicrosoftEdge\Main",
                Key = "FormSuggest Passwords",
                Value = "no"
            });
            edge.Add(new Tweak("Disable First Start Page", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\MicrosoftEdge\Main",
                Key = "PreventFirstRunPage",
                Value = 1
            });
            edge.Add(new Tweak("No Form Auto Complete", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\MicrosoftEdge\Main",
                Key = "Use FormSuggest",
                Value = "no"
            });
            edge.Add(new Tweak("Disable AddressBar Sugestions", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\MicrosoftEdge\SearchScopes",
                Key = "ShowSearchSuggestionsGlobal",
                Value = 0
            });
            edge.Add(new Tweak("Disable AddressBar (drop down) Sugestions", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\MicrosoftEdge\ServiceUI",
                Key = "ShowOneBox",
                Value = 0
            });
            edge.Add(new Tweak("Keep New Tabs Empty", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\MicrosoftEdge\ServiceUI",
                Key = "AllowWebContentOnNewTabPage",
                Value = 0
            });
            edge.Add(new Tweak("Disable Books Library Updating", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"Software\Policies\Microsoft\MicrosoftEdge\BooksLibrary",
                Key = "AllowConfigurationUpdateForBooksLibrary",
                Value = 0
            });


            // *** Lockdown IE ***

            Group ie = new Group("Lockdown IE");
            miscCat.Add(ie);
            ie.Add(new Tweak("Disable Enchanced AddressBar Sugestions", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Internet Explorer",
                Key = "AllowServicePoweredQSA",
                Value = 0
            });
            ie.Add(new Tweak("Turn off Browser Geolocation", TweakType.SetGPO, WinVer.Win7)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Internet Explorer\Geolocation",
                Key = "PolicyDisableGeolocation",
                Value = 1
            });
            ie.Add(new Tweak("Turn off Site Suggestions", TweakType.SetGPO, WinVer.WinXP)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Internet Explorer\Suggested Sites",
                Key = "Enabled",
                Value = 0
            });
            ie.Add(new Tweak("Turn off FlipAhead Prediction", TweakType.SetGPO, WinVer.Win8)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Internet Explorer\FlipAhead",
                Key = "Enabled",
                Value = 0
            });
            ie.Add(new Tweak("Disable Sync of Feeds & Slices", TweakType.SetGPO, WinVer.WinXP)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Internet Explorer\Feeds",
                Key = "BackgroundSyncStatus",
                Value = 0
            });
            ie.Add(new Tweak("Disable Compatybility View", TweakType.SetGPO, WinVer.WinXP)
            {
                Path = @"Software\Policies\Microsoft\Internet Explorer\BrowserEmulation",
                Key = "DisableSiteListEditing",
                Value = 1
            });

            ie.Add(new Tweak("Disable ActiveX Black List", TweakType.SetGPO, WinVer.WinXP)
            {
                usrLevel = true,
                Path = @"Software\Policies\Microsoft\Internet Explorer\BrowserEmulation",
                Key = "DisableSiteListEditing",
                Value = 0,
                Hint = Tweak.Hints.Optional
            });
            ie.Add(new Tweak("Disable First Run Wizard", TweakType.SetGPO, WinVer.WinXP)
            {
                usrLevel = true,
                Path = @"Software\Policies\Microsoft\Internet Explorer\Main",
                Key = "DisableFirstRunCustomize",
                Value = 1
            });
            ie.Add(new Tweak("Set Blank Stat Page", TweakType.SetGPO, WinVer.Win2k)
            {
                usrLevel = true,
                Path = @"Software\Policies\Microsoft\Internet Explorer\Main",
                Key = "Start Page",
                Value = "about:blank"
            });
            ie.Add(new Tweak("Keep New Tabs Empty", TweakType.SetGPO, WinVer.WinXP)
            {
                usrLevel = true,
                Path = @"Software\Policies\Microsoft\Internet Explorer\TabbedBrowsing",
                Key = "NewTabPageShow",
                Value = 0
            });
 


            /*  
             *  #########################################
             *              Apps & Store
             *  #########################################
             */

            Category appCat = new Category("Apps & Store"); //, "store_and_apps");
            Categorys.Add(appCat.Name, appCat);


            // *** Disable Store ***

            Group store = new Group("Disable Store");
            appCat.Add(store);
            store.Add(new Tweak("Disable Store Apps", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\WindowsStore",
                Key = "DisableStoreApps",
                Value = 1
            });
            store.Add(new Tweak("Don't Auto Update Apps", TweakType.SetGPO, WinVer.Win10EE)
            {
                Path = @"SOFTWARE\Policies\Microsoft\WindowsStore",
                Key = "AutoDownload",
                Value = 2
            });
            store.Add(new Tweak("Disable App Uri Handlers", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"Software\Policies\Microsoft\Windows\System",
                Key = "EnableAppUriHandlers",
                Value = 0,
                Hint = Tweak.Hints.Optional
            });


            // *** Lockdown Apps ***

            Group apps = new Group("Lockdown Apps", true);
            appCat.Add(apps);
            apps.Add(new Tweak("Don't Let Apps Access AccountInfo", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Key = "LetAppsAccessAccountInfo",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Calendar", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Key = "LetAppsAccessCalendar",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access CallHistory", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Key = "LetAppsAccessCallHistory",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Camera", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Key = "LetAppsAccessCamera",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Contacts", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Key = "LetAppsAccessContacts",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Email", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Key = "LetAppsAccessEmail",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Location", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Key = "LetAppsAccessLocation",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Messaging", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Key = "LetAppsAccessMessaging",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Microphone", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Key = "LetAppsAccessMicrophone",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Motion", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Key = "LetAppsAccessMotion",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Notifications", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Key = "LetAppsAccessNotifications",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Radios", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Key = "LetAppsAccessRadios",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access Tasks", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Key = "LetAppsAccessTasks",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Access TrustedDevices", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Key = "LetAppsAccessTrustedDevices",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps get Diagnostic Info", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Key = "LetAppsGetDiagnosticInfo",
                Value = 2
            });
            apps.Add(new Tweak("Don't Let Apps Run In Background", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Key = "LetAppsRunInBackground",
                Value = 2,
                Hint = Tweak.Hints.Optional
            });
            apps.Add(new Tweak("Don't Let Apps Sync With Devices", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                Key = "LetAppsSyncWithDevices",
                Value = 2
            });


            // *** No Mail and People ***

            Group mail = new Group("Block Mail and People", true);
            appCat.Add(mail);
            mail.Add(new Tweak("Disable Mail App", TweakType.SetGPO, WinVer.Win10)
            {
                Path = @"SOFTWARE\Policies\Microsoft\Windows Mail",
                Key = "ManualLaunchAllowed",
                Value = 0
            });
            mail.Add(new Tweak("Hide People from Taskbar", TweakType.SetGPO, WinVer.Win10)
            {
                usrLevel = true,
                Path = @"Software\Policies\Microsoft\Windows\Explorer",
                Key = "HidePeopleBar",
                Value = 1
            });

            // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8}_NvTelemetry
            //"C:\Windows\SysWOW64\RunDll32.EXE" "C:\Program Files\NVIDIA Corporation\Installer2\InstallerCore\NVI2.DLL",UninstallPackage NvTelemetry

            // HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\OneDriveSetup.exe
            //taskkill /f /im OneDrive.exe
            //%SystemRoot%\System32\OneDriveSetup.exe /uninstall
            //%SystemRoot%\SysWOW64\OneDriveSetup.exe /uninstall

            //[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Defender\Features]
            //"TamperProtection" = dword:00000000

            return true;
        }
    }
}
