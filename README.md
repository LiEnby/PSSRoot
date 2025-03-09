One-Click-Root program based on CVE-2016-5195 or "DirtyCOW";

this should work with the PlayStation Certified devices, 
but it may come in handy for other old android devices too;

Tested on :

Xperia Play (Android 2.3; Kernel 2.6.32.9)
Xperia S (Android 4.1.2; Kernel 3.4.0+1.0.21100-313065)
Sony Tablet P (Android 3.2; Kernel 2.6.36.3)

you may need the adb drivers for your device, in the case of sony's one its:
https://developer.sony.com/open-source/aosp-on-xperia-open-devices/downloads/drivers
you will also need USB Debugging enabled;

CVE-2016-5195 lets you overwrite any file that you have read access too, regardless of if it has write permission;
we use this to temporarily overwrite /system/bin/run-as which always runs as root, to then install su
for this reason its recommended to not close the application and ensure a good connection to ADB;

[LiveOverflow did a video on this particular vulnerability](https://youtube.com/watch?v=Lj2YRCXCBv8)

reason this can't be its own standalone app is that /system/bin/run-as is the only SUID binary present in older android versions;
and it's only readable and executable from the 'shell' user, not within apps; meaning you have to trigger it from ADB Shell.


