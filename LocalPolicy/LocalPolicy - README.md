# README #

See [this blog post](http://www.lshift.net/blog/2013/03/25/programmatically-updating-local-policy-in-windows/) for an introduction to the library.

Please note, when using this library your program needs to run

* As a single-threaded apartment. This means decorating your Main method with the `[STAThread]` attribute. See [here](http://msdn.microsoft.com/en-gb/library/windows/desktop/ms680112(v=vs.85).aspx) for more documentation.
* With administrator privileges