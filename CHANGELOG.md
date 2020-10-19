# Changelog
All notable changes to this project will be documented in this file.
This project adheres to [Semantic Versioning](http://semver.org/).


## [0.83] - 2020-10-19

### Added
- added fitlering mode indocator to tray
- tweak change notification are not displayed in an own notification window tab
- added new access presets InBoundAccess and OutBoundAccess
- added rule hit counter
- added protocol filter to connection log

### Changed
- cleaned up some old code
- now all new connection notifications can be discarded at once
- moved firewall API to own library
- refactored the code, improved IPC structure

### Fixed
- fixed IPC issue with DNS blocklist
- fixed issues with programwnd
- fixed some app package ID's not being resolved
- fixed high cpu load when sorting by program column
- fixed potentiel crash in GetAppResourceStr



## [0.82b] - 2020-10-17

### Changed
- tray icon is now on by default 

### Fixed
- fied resource lea when flashign tray icon



## [0.82] - 2020-10-13

### Added
- added option to quickly add (pin) firewall prog sets to presets
- added option to quickly add (pin) tweak groups to presets
- added flasching exclamation mark to tray icon to indicate there are pending notifications
- added tweaks to disable app auto installation

### Fixed
- fixed issue with port ranges in firewall rules thanks@ SnikoLoft
- fixed issue with saving preset items
- fixed issue with notification window position being reset sometimes



## [0.81] - 2020-10-11

### Added
- added a few for windows update
- added tray options to change filterming mode
- added rule status column

### Changed
- when adding a rule from the notify window the rule window is also topmost
- notify window now also shows allowed connection attempts when in blacklist mode

### Fixed
- added service update to installer
- fixed rule list not being updated in fulscreen mode
- fixed some new issues with the IPC mechanism



## [0.80] - 2020-10-07

### Added
- added a few more tweaks
- added new preset control mechanism

### Changed
- All microsoft binaries under System32 are now classifyed as system components (not only services and kernel)
- moved shared code to a helper library
- splited the project in separated UI and service components
- reingeniered the IPC mechanism for better performance
- notification window now displays all changed rules also those that changed when the client wasn't running
- reworked client start

### Fixed
- list sometimes opening in the wron mode
- fixed issue with program and program set description resolvationo
- fixed some crash issues



## [0.75b] - 2020-03-20

### Fixed
- This release fixes a crash issue with the notification window.


## [0.75] - 2020-02-18

### Added
- when a firewall rule gets changed a new notification pupup gets displayed
- notification window has now tabs
-- added tab with rule changed notifications
- notification window can now be opened/closed by single clicking on the tray icon, without discarding contents

### Fixed
- settings backups names dont longer contian ':'


## [0.74] - 2019-12-21

### Changed
- changed service tag handling to only apply to svchost.exe hosted services
	- all other services will only be handled as regular programs identifyed by their path
	- the program window now by default always sets the service binary path when a service is selected

### Fixed
- when opening the program window comboboxes were not properly disabled 
- fixed issue with updating service PID cache


## [0.73] - 2019-12-19

### Added
- dns proxy blockist is now saved every 15 minutes
- added greatly improved search edit box, focus with ctrl+f
- added "del" keyboard short key to remove selected item

### Changed
- reworked GPO handling to avoid write lock conflicts on slower machines

### Fixed
- fixed an issue when clicking the tray icon before the main window was fully loaded
- fixed access color not changing in program list view
- fixed crash bug on start as on admin
- fixed crash bug with app package name resolution
- fixed issue when upon a change the ribbon controls were not updated acordingly


## [0.72] - 2019-12-17

### Added
- German Translation by uDEV2019 
- added option to backup and restore ptiv10 settigns from/to file

### Changed
- priv 10 ui does nto logner offer to stop the serive when closing from tray but not running as admin
- when running in portable mode data are not longer stored in the application directory directly 
	but in the ".\data" sub directoy, when running in portable mode its needed to manyualy move theconfig files when updating

### Fixed
- fixed an issue with gul guard setting
- some englisch spelling corrections by CHEF-KOCH


## [0.71] - 2019-12-16

### Added
- added side bar button tooltips
- added cleanup options for DNS inspector
- added cname host mane display

### Changed
- when sellecting the "All processes" placeholder entry the detail tabs (except rules) shows data of all processes
- reduced cpu usage when sorting the program tree
- improved firewall settign handling
- changed settings layout
- reworked app package handling to peoperly operate as a service
- simple list is now availabel also in "full height" view mode

### Fixed
- issue with socket associaction resulting in memory leak
- issues with rule guard enaling/disabling
- fixed issues when running priv 10 not as admin
- fixed issue with DNS cache
- fixed minor issue with process monitor commandline handling




## [0.70] - 2019-12-14

### Added
- improved the DNS Inspector
	- added uplpad/download per domain
	- added listing of unresolved IP's
	- added udp endpoint logging
- added proces monitor using ETW events to capture paths of processes that terminate quickly
- added missing localization to all the new UI elements
- added todal up/down-load per socket and per program columns

### Changed
- improved text filters

### Fixed
- fixed issue with reverse dns in dns inspector
- fixed issue with etw event tracking for UDP traffic



## [0.65.2] - 2019-12-13

### Fixed
- fixed crash when deleting program items
- fixed issue with program cleanup ribbon bottons
- fixed issues with manual blocklist update
- fixed inconsistencies with access mode filter
- fixed issue installer was instaling to "Program Files(x86)" instead of "Program Files", uninstall and re install to fix the path
- fixed some translation strings



## [0.65.1] - 2019-12-10

### Added
- added a propper setup
- added setting for using reverse DNS

### Changed
- changed drive letter resolcong cache strategy

### Fixed
- fixed service not proeprly terminating
- fixed issues with service status querying
- fixed issue with notification window oppening unnececerly on a delayed hostname update


## [0.65] - 2019-12-04

### Added
- added new program view mode a verbose program tree, that auto enables when the program column ist stretched wider
- added program context menu
- added additional program options to the ribbon toolbar
- added view modes fill screan, full height, full screen 
- dns query log context menu with options to whitelist and blacklist entries
- double clicking a domain in the whitelist/blacklist view copys it to the entry edit for editing



## [0.65] - 2019-12-04

### Added
- added new program view mode a verbose program tree, that auto enables when the program column ist stretched wider
- added program context menu
- added additional program options to the ribbon toolbar
- added view modes fill screan, full height, full screen 
- dns query log context menu with options to whitelist and blacklist entries
- double clicking a domain in the whitelist/blacklist view copys it to the entry edit for editing

### Changed

### Fixed

## [0.60.1] - 2019-12-01

### Added
- added process monitor, using ETW events to aid resolving PID to program, when the prgram in question already exited.
- added upstream dns diplay to the dns proxy page

### Changed

### Fixed
- fixed critical hang issue when not using the system event log
- issue where a custom upstream dns would not be displayed
- fixed issues with copying text from datagrids


## [0.60] - 2019-11-30

### Added
- added DNS proxy to monitor all DNS requests
- added ability to set priv10 as system DNS 
- added DNS Query Log based on DNS Proxy events
- added system DNS cache monitor
- added dns proxy page 
	- added dns querry log
	- added dns blacklist/whitelist page
	- added dns blocklists page
- added blocklist update mechanism

### Changed
- reworked DNS Inspector class
- changed from tne .NET reverse dns facility to using native windows API's for better performance
- dns inspector can now be switched on or off

### Fixed
- issue where the accept button in the notifiction window would get disabled again 
- issue with aplpyung rules when some rules were already present for the given programm
- issue saving gpo's, sometimes it failed with a file is in use error
- issue where file blockign tweaks were never shown as available after a recent change



## [0.57] - 2019-11-16

### Added
- added a few new tweaks
- aded windows 8 and newer address keywords
- added app context menu option to uninstall the client (remove service, autorun, event log)
- added -help command to show all available console ocmmands
- added clear firewall log option
- added extended program cleanup option

### Changed
- on windows 8 and above the "Internet" address keyword is used for rules, instead of a manually assembled range
- rule lookup now properly handles the interface type parameters
- improved special address handling
- priv10 is now by default only using the windows event log when being installed as a service

### Fixed
- fixed issue with grouping combobox
- fixed issue with original rule backup



## [0.55] - 2019-11-15

### Added
- added tweaks to disable visual studio tlemetry
- when closing priv10 from tray now it prompts if the user wants to stop the priv10 service
- programs.xml and tweaks.xml is now auto saved every 15 minutes
- added rule context menu
- added command to restore rules detected as changed
- added command to approve rules detected as changed
- program list cleanup not removed not longer existing programs
- added access filter to view options
- added socket type filter to socket list
- added access column to socket list
- apps/services/groupes in dropdown are now sorted alphabetically

### Changed
- moved rule quick actions to ribbon (commands are also avialavle form context menu)
- moved panel filter options to toolbar form sidebar
- reworked change notifications
- rule change notifications now display the resolved name when possible

### Fixed
- issues with some tweak definitions
- blocked/allowed connetion display was mixed up
- fixed minor issue with event log notificaiton
- fixed issue when changing rule guard mode
- fixed crash bug in notifications
- fixed issue adding apps


## [0.50b] - 2019-11-05

### Added
- compatybility with old 32 bit platforms

### Fixed
- when rule guard is not enabled all rules were marked as changed



## [0.50a] - 2019-11-03

### Added
- added own event log, displayed in the overview page
- added network manager listing all open sockets and currently connected networks
- added ETW tracking to obtain datarate information for sockets
- added dns inspector, dns query monitoring with fallback to reverse dns queries
-- showing domain names to the remote adresses
-- logging all domains every program tryed to connect to
- added tweaks to disable office 2016/2019 telemetry
- added Ribbon Toolbar to the Firewall view
-- improved program list filtering a lot
-- added more list view customization options
-- much better category filtering
-- etc...
- added Tweak Guard to automatically re apply tweaks that windows undid
-- tweaks and their states are saved to tweaks.xml
-- tweak changes are logged on the priv10 event log
- added Firewall Rule Guard
-- firewall rule changes are identifyed on start as well as monitored on live during runtime
-- Optin to automatically disable changed/added rules or to flat out always out undo all changes not done by priv10
-- All 3rd party changes to firewall rules logged in the priv10 event log + notfication
- added option to reload app list from apps dropdown
- added propper modern app resource string display


### Changed
- completly reworked the backend (engine and service) code
- refactored IPC code
- dropped many unnececery dependencies
- Now by default using "C:\ProgramData\PrivateWin10" instead of the application directory to store data
-- If a PrivateWin10.ini is present in the applicaton directory the tool starts in portable mode instead
- reworked firewall manager and firewall rules
- reworked firewall event monitoring to make it more robust to future changes in windows
- Changed from the old and out dated COM Interface to a use Native windows firewall API's instead
-- Now supporting new windows 10 features
- reworked datagrid  header handling

- and much more that I forgot to write down...

### fixed
- program list browsing with arow keys when filter is engaged
- reworked AppManager class to properly work
-- improved sid to app resolution performance
-- properly resolving apppackage id of running modern apps by current process ID
- not uniqie service resolution is now handled properly.
- sorting by timestamp now works properly
- fixed issues with file and registry tweaks not being un don properly
- fixed issues with not applicable tweaks being sometimes shown where it could have been avoided

- and many more which I forgot to write down...




## [0.1h] - 2019-05-01

### Added
- Compatybility with windows 8.1 and server 2012
- first seen date for programs

### Fixed
- diagtrackrunner.exe and AeLookupSvc tweak are not longer offered to win10 users as on win 10 thay are not present
- crash when adding new category

## [0.1g] - 2019-01-06

### Added
- notification window has now path label clickable and opens explorer in the files directory
- issuing cleanup now also resets teh error flag

### Changed
- disabled autoresize for Name column
- open from tray trayicon now only react to left mouse button double click
- notification window direction stings are nor more clear
- allowed lan connections are now logged blue
- allowed multicast connections are now logged turquoise

### Fixed
- notification window next/previouse buttons now peoeprly enable/disable
- fixed crash when listing a not longer existing service
- fixed issue when resolving special adress ranges

## [0.1f] - 2019-01-03

### Added
- italian translation by garf02
- prevent starting of multiple instances inside the same user session
- add option to block/unblock internet access from tray

### Fixed
- not showing proepr "any protocol" string in lists
- rule violating actions are not colored yellow
- fixed ptiv10 failed to start when firewalls ervice was disabled

### Changed
- port and Ip columns doesn't autoresize anymore

## [0.1e] - 2018-12-29

### Fixed
- localization string screwup in firewall page
- englich fixed by spamtrash

### Added
- polish translation by spamtrash (programyzadarmo.net.pl)


## [0.1d] - 2018-12-28

### Added
- save last open page and open it on restart
- all setings can now enabled when running not as dmin, wne a admin only seting is to be chaged the cleint prompts fo a restart
- finish localizations upport

### Fixed
- fixed issue in retriving service by PID
- UAC bypass messing with -autorun argument

### Changed
- improved emabling and disabling of execution as service


## [0.1c] - 2018-12-27

### Added
- buttons in the firewallwindow get enabled/disabled based on list selection
- rule validation for the rule window

### Fixed
- not setting properly local port and address when creating a rule from the notification window
- fixed a bug in ip matching when a subnet was present
- rule window not saving position
- crash issue under windows 7 related to non existent app list

### Changed
- changes the way service names and PIDs are resolved


## [0.1b] - 2018-12-25

### Fixed
- possible crash when loading the list off installed uwp apps


## [0.1] - 2018-12-23

### Added
- Initial release
