# PROJECT NOTES

## Definite todos

- [ ] Use machine learning to provide input tyre pressures.
  - [See more below...](#input-tyre-pressures)
- [ ] Detect punctures and add the amount to db.
    - [x] Add implementation
    - [ ] Test implementation
- [ ] Rework loading of previous data to be more representative of current condition.
    - [ ] How to handle not enough data? Widen requirements? Don't load?
- [ ] Use broadcast data to read air and track temp while in pits or race start as they are 0 then in shared memory.
- [ ] Add options to enable/disable different components, like colors, min/max/avg/std
- [ ] Add graphical settings manager inside SimHub
- [ ] Test performance.
    - Regular update seems to take ~0.1ms, which I think is ok. If SimHub runs at 60fps, it gives 1000ms/60=16.7ms for one update.
    - Most expensive are db commits. Takes around 3-10ms which is a bit much.
        - Tried to use single transaction for lap and stint inserts. Works sometimes but not always. Figure out why.
            - Is there any query between which messes up our single transaction?
            - Something random?
            - With single transaction, lap finish takes around 0.5ms
        - Cache inserts statements into list and insert later?
            - In this case we need to keep track of tyre sets and laps driven with each tyre set. Maybe something else?
#### *DONE!*

- [x] Remove outliers from the lap and fuel data
    - [x] How to handle ecu changes? 
    
        Don't remove laps from fuel data, lap times shouldn't change that much (unless you use pacecar map, which I suppose realisticaly would never happen)
    - [x] How to deal with changing conditions? 

        Condition changes from cold->hot should be within range defined above. Fuel variations seem to be within the range always, except for real outliers.

        If we go dry->rain the laptimes get slower and may be outside upper bound. However it should be safe as we estimate more laps than actually is possible.

        On the other hand rain->dry we estimate less laps for the race and thus fuel calculation is wrong. We could only remove upper outliers?
    - Remove upper outliers for laps and none for fuel. 
    
        The reason is that lower laptimes are not usually outliers (unless we cut the track), this also removes the problem with going from rain->dry where we would estimate too few laps for the race. 
    
        We remove none from fuel because you need to be doing something quite weird to mess up fuel calculation but ecu changes could potentially be outside the bounds set from IQR. 
        
        For example crashing doesn't change fuel consumption that much whereas laptime is much lower.

        Note that changes in track conditions are then represented by min/max values and average takes some time to catch up to real conditions, but I suppose that's logical.
           
          

## Ideas

- Separate colors to separate plugin? Altough much of logic would need to be same, eg when to reload color values.
- Something else useful in broadcast data?

## Outlier detetion in FixedSizeDequeStats

- Use stats from all data - need to query a lot, probably not?
- Use stats from last values - maybe not enough to reliably detect outliers?

## Machine learning


### Input tyre pressures

- Check initialization and updating of models.
- Check that have enough data with enough spread, eg temperature difference of at least 5 degrees.
- Outliers in data
  - Different ecu maps, one model for each map? Probably is fine for map 1/2 but lower should be removed.
  - How to detect slow laps behind someone?
    - Maybe some combination of gaps from the broadcasting data (can actually use Swoop plugin) and lap time difference to avg/best?
    - Remove laps with position change and
  - Remove all outlier laptimes based on IQR?
  - Remove pressure outliers based on IQR from given stint?
  - Remove all laps after punctures.
- When should we refit model?
  - Maybe should differ depending on how much data we have? Less data mean that each new point is critical and we should learn more often. If we have a lot of data it doesn't matter that much and we could learn at the end of session or something similar.
  - At the end of each lap?
  - At pit?
  - At rtg?
  - At end of session?
- Implement multiple models if there is enough data in certain range since the data is not quite linear (probably).
  - Was thinking of ranges -15, 15-20, 20-25, 25-30, 30-35, 35- airtemp.
  - Use both models in overlap region?
  - If not enough data in given range, it will fallback to larger range.
- Should we normalize data to unit variance and zero mean?
- How to deal with different brake ducts?
  - Different models?
  - Some non-linear model?
  - Can it be transformed to linear model? Tried sometihing but wasn't able to.
- Learn pressure drop across the stint.
  - If it's significant how many laps should be used to learn input tyre pressures?

#### *DONE!*

- [x] Use python or ML.NET?
  - Instead wrote RidgeRegression in C#
- [x] Test if pressure change is linear with temp.
  - [x] Drive laps with same input pressures but different air/track temps.
  - [x] Drive laps with same temps but different input pressures.
  - Seems to be at least very close to linear in combination or air/track temperature. Multiple linear models with smaller temp range should work very well.
- [x] Can model just depend on track temp? 
  - No, because air temp affects brake cooling which in turn affects tyre temps and pressures. 
  <br>
  Eg higher air temp -> hotter brakes -> higher tyre temps/pressures.

### Fuel use with different temps

- Seems to change, larger usage with higher temps

### Lap times

### Tyre use

### Race simulation