### FAQ

**Q:** What is this? A replacement for Vortex?

**A:** Eventually, yes. But not for quite some time, at the moment this software should be considered "pre-alpha". Do not ask for support for these tools until official announcements and a product launch is made. Feel free to direct any questions to Halgari in the meantime.


**Q:** Why is this being done, why a new platform, design, etc.?

**A:** Vortex was designed primarily for development by a team of one person (Tannin), as at the time Nexus wasn't ready to take on the financial and leadership burden of a larger development team. However, times change, and we're now in the position of being able to have a more well-structured team and development schedule. As such we are increasing the size of our mod management team. On the technical side of things we're taking time to lay a solid CI foundation, setup a well-rounded data model (using lessons we've learned over the years of developing Vortex), and build this project to be more of a company product and less of a community project.


**Q:** Company product? Is this going closed source?

**A:** Absolutely not, modding tools should be free, and the Nexus Mods App will always be open source (GPL3). But we also want to use this app as a way to get tools into the hands of modders and users. Instead of publishing only a download API, we want to give users a download CLI tool. Once file uploading is reworked on the site, this repo will contain the code and CLI tools required for authenticating with Nexus Mods and uploading files via a CLI (and eventually a UI). In short, this is us getting serious about supporting (and leading) the desktop side of modding, not just the file hosting side.


**Q:** I see tests run on Linux, Windows, and OSX; are you targeting all those platforms?

**A:** Yes, the CLI runs on these platforms, and we run our CI on each of these OSes. What games are supported on these platforms (e.g. do we support Skyrim through Wine on Linux?) is yet to be determined.
