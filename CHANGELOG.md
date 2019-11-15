# Changelog
All notable changes to this project will be documented in this file.
This project adheres to [Semantic Versioning](http://semver.org/).

## [0.51] - 2019-11-15

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
