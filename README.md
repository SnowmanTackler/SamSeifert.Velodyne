# SamSeifert.Velodyne

This is a C# Library for Velodyne Products, although it currently only supports VLP-16.

There is a reference to my own personal utilities library, which is available here: https://github.com/SnowmanTackler/SamSeifert.Utilities .  Put both the SamSeifert.Velodyne and SamSeifert.Utilities folders in the same parent directory, and the "example.sln" should find everything it needs.  You preferably should call the Listen() methods in a background thread!