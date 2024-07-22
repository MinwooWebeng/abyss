** Thread safety notice

* do not call Host.ListenAndServeAsync() multiple times.
* do not call other methods of Host before Host.ListenAndServeAsync() returns