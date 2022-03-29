﻿# Definite todos

- [ ] Use machine learning to provide expected fuel consumption, for now and future conditions.
- [ ] Add graphical settings manager inside SimHub
- [ ] Learn fuel usage with different temperatures and track conditions.
- [ ] Input tyre pressures
    - [ ] Precalculate tyre pres model coefficients after the event, as it could take some time if database grows.
    - [ ] Check initialization and updating of models.
    - [ ] Check that have enough data with enough spread, eg temperature difference of at least 5 degrees.
    - [ ] Outliers in data
      - [ ] Different ecu maps, one model for each map? Probably is fine for map 1/2 but lower should be removed.
      - [ ] How to detect slow laps behind someone?
        - [ ] Maybe some combination of gaps from the broadcasting data (can actually use Swoop plugin) and lap time difference to avg/best?
        - [ ] Remove laps with position change and
      - [ ] Remove all outlier laptimes based on IQR?
      - [ ] Remove pressure outliers based on IQR from given stint?
      - [x] Remove laps with large punctures, add puncture amout back for small punctures (say below 0.25).
    - [ ] When should we refit model?
      - Maybe should differ depending on how much data we have? Less data mean that each new point is critical and we should learn more often. If we have a lot of data it doesn't matter that much and we could learn at the end of session or something similar.
      - At the end of each lap?
      - At pit?
      - At rtg?
      - At end of session?
    - [ ] Implement multiple models if there is enough data in certain range since the data is not quite linear (probably).
      - Was thinking of ranges -15, 15-20, 20-25, 25-30, 30-35, 35- airtemp.
      - Use both models in overlap region?
      - If not enough data in given range, it will fallback to larger range.
    - [ ] Should we normalize data to unit variance and zero mean?
    - [ ] How to deal with different brake ducts?
      - Different models?
      - Some non-linear model?
      - Can it be transformed to linear model? Tried sometihing but wasn't able to.
    - [ ] Learn pressure drop across the stint.
      - If it's significant how many laps should be used to learn input tyre pressures?
    - [ ] Pres predictor
        - [ ] Limit ourselves to five possible models. These are main weathers and any interim condition will be left for player to change accordingly.
            - Dry tyre + dry track
            - Dry tyre + greasy track
            - Wet tyre + damp track + drizzle
            - Wet tyre + damp track + light rain
            - Wet tyre + wet track + medium rain
            - Wet tyre + wet track + heavy rain
            - Wet tyre + flooded track + thunderstorm
        - [ ] Give three predictions:
            - Current track conditions
            - In 10 min
            - In 30 min
        - [ ] For future predictions predict track conditions from the weather.
            - If time multiplier is larger than 3
                - If track is wet now, it will be wet for all future weather
                - If track is damp now
                    - Then future med/heavy rain -> track is wet
                    - Then future drizzle/light rain -> track is damp
                    - Then future dry
                - If track is dry now
                    - Then future Drizzle/light rain -> track is damp
                    - Then future med/heavy rain -> track is damp
                    - 
            - If weather if medium/heavy rain -> track is wet
            - If weather if thuderstorm -> track is flooded
            - If now is medium/heavy rain
                - If in 10 min is light rain -> track in 10 min is wet
                - If in 
    - [x] Use python or ML.NET?
        - Instead wrote RidgeRegression in C#
    - [x] Test if pressure change is linear with temp.
        - [x] Drive laps with same input pressures but different air/track temps.
        - [x] Drive laps with same temps but different input pressures.
        - [x] Seems to be at least very close to linear in combination or air/track temperature. Multiple linear models with smaller temp range should work very well.
    - [x] Can model just depend on track temp? 
        - [x] No, because air temp affects brake cooling which in turn affects tyre temps and pressures. 
        <br>
        Eg higher air temp -> hotter brakes -> higher tyre temps/pressures.

#### *DONE!*

- [x] Try to get rid of as much pm.GetProperty as possible. That thing is quite slow.
- [x] Move database interactions to different thread or use async
  - [x] Passed inserts off to separate threads. Joining every thread before next interaction with db.
  - [x] How to pass of queries to separate threads? Synced by mutex as they are not on main thread, possible interactions are quite far apart (in time) and thus we can afford a simple mutex.
- [x] Use separate thread to learn tyre pres models
- [x] Store laps on different tyresets in a map to get rid of the query to db.
- [x] Use broadcast data for track grip status - no such data available
- [x] First call is slow - C# uses JIT compilation, added PreJit method to compile all methods at RaceEngineerPlugin.Init
    - First data update takes long (~100ms) but it's okay as nothing happend in game then.
    - Regular update seems to take ~0.1ms, which I think is ok. If SimHub runs at 60fps, it gives 1000ms/60=16.7ms for one update.
    - First lap insert to db takes also longer, I suppose there is some allocations going on which are reused later, maybe at assigning parameters.
    - First insert to FixedSizeDeque seems to be a lot longer than others (is Deque lazily initialized?). Same thing for third insert on which we start calculating IQR.
- [x] Detect ecu map changes mid lap, and add boolean to db, such laps should be excluded from fuel calculation. If change is to or from fuel save maps, should alse exlude from tyre pres calculations.
- [x] Add booleans (one per tyre) to db that this lap was puncture lap, it means that it should be exluded from tyre pressure calculation as the data is skewd.
- [x] Use broadcast data to read air and track temp while in pits or race start as they are 0 then in shared memory.
- [x] ~~For calculation of input tyre pressures from current avg tyre pressures use machine learned delta values. That is how much does change in input pressure change hot pressures. This is needed as 0.1 psi input change doesn't exactly result in 0.1 psi hot pressure change.~~
  - ~~How to learn it?~~
      - ~~This delta depends on air temp, track temp and hot/input pressure (seems to be that for higher input pressures the delta is smaller)~~
      - ~~We could learn hot pressure from air temp, track temp and input pressure and then vary input pressure to get the change.~~
   - Actually it seems to be roughly the same.  
- [x] Add options to enable/disable different components, like colors, min/max/avg/std
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
- [x] Detect punctures and add the amount to db.
    - [x] Add implementation
    - [x] Test implementation