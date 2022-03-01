# TODOS

* Add options to enable/disable different components, like colors, min/max/avg/std

* Add graphical settings manager inside SimHub

* Use broadcast data to read air and track temp while in pits or race start as they are 0 then in shared memory.

* Test performance.

# IDEAS

* Separate colors to separate plugin? Altough much of logic would need to be same, eg when to reload color values.

* Something else useful in broadcast data?

# Outlier detetion in FixedSizeDequeStats
* Use stats from all data - need to query a lot, probably not?
* Use stats from last values - maybe not enough to reliably detect outliers?

# Input tyre pressure learning

* ~~Use python or ML.NET?~~ Wrote RidgeRegression in C#
* Check initialization and updating of models.
* Check that have enough data with enough spread, eg temperature difference of at least 5 degrees.
* When should we refit model? 
	* At the end of each lap? 
* Implement multiple models if there is enough data in certain range since the data is not quite linear (probably).
	* Was thinking of ranges -15, 15-20, 20-25, 25-30, 30-35, 35- airtemp. 
	* Use both models in overlap region?
	* If not enough data in given range, it will fallback to larger range.
* 
* Should we normalize data to unit variance and zero mean?
* How to deal with different brake ducts?