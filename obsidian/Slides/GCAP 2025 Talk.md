---
notesSeparator: SPEAKER_NOTES
width: "1440"
height: "1200"
timeForPresentation: 3000
defaultTemplate: "[[tpl-default]]"
---

# Every tree in the forest
Dynamic audio clustering


---
# Introduction
> [!info] Navigation: These slides are on a grid
> Go down to get to the next slide in a section
> 
> Go right to get to the next section
<!-- element style="font-size:50%;text-align:left;width:60%"-->

--
## Who I am

> [!error] Kiera Lord  (@Cratesmith)  
> Senior Gameplay Programmer @ Gameloft Brisbane
> 
> Queer, trans, heavily austistic, generalist developer  
<!-- element style="font-size:75%;width:60%"-->
--
### Some stuff I've done
Not an exaustive list. But it gets across I've been doing this for a while now.

-  PS2/XB: **Destroy All Humans 2**
-  360/PS3: **The Dark Knight (Unreleased)**
-  Wii: **Next big thing (Unreleased)**
-  Web: **Alternator**
-  PC/PS3: **Vessel** <!-- element style="color:cyan""-->
-  iOS/Droid: **Sim Cell**
-  iOS/Droid: **Codebreakers**
-  PC/Mac: **Kinect & VR support for Tail Drift**
-  GearVR: **Small**  **(unreleased)**
-  Cardboard: **RACQ Bike VR**
-  GearVR: **Zombie Nom Nom**
-  Switch/PS4/Xbone: **Windbound**
-  PC: **Cosy Caravan**

--

### What to expect from this talk
  
* My goal is that everyone to get a high level of how everything works, but not all the details. 
	* Programmers I'm hoping will understand how the system work in detail 
	* Audio designers I'm hoping will understand enough how design sounds/events/parameters to work with the system.
<br/>  
* Like all my talks, this is going to be an onslaught of information. Don't feel like you need to understand every last detail.

* This presentation is an introduction, with the slides/demo/recordings/source code containing more detail that you can refer to later.
<br/>
* Some parts of this talk will be a bit rough. There really wasn't enough time to finish the demo I wanted to prepare AND redesign this talk to fit in the audio track.

--

## Moving on
Lets get this started shall we? 

---
# The problem we're trying to solve

--

## Volumetric audio is hard

* It's where we want an area to play a sound as if it occupies an area, or we're playing a sound to represent many things in an area.

* Some examples:
	* Liquid simulations
	* Ambience for dense foliage
	* Sounds for crowds/traffic/mass characters
	* Large numbers of collision or impact sounds
<br/>
* We can't just play more sounds to fill the space/for each point.
	* We'd use up cpu/memory/available voices
	* It'd sound terrible due to phasing/destructive interference etc.
<br/>
* Common solutions to these problems are
	* Ambience: Hand placed audio emitters or ambience volumes. 
	* Mass Sounds: Bespoke, feature specific systems to play each sound
	* Fallback: Reduce maximum voice count and just hope for the best
--
## The solution we're exploring

* Instead of having one sound per thing, we want to group them based on proximity and play a shared sound to represent multiple things at once.
<br/>  
* We give that sound a parameter for "number of things" so the sound can change depending on how many their are (plus any other parameters the sound needs)
<br/>
* A good example of this is bullet impact sounds from a minigun. If we played a unique sound for each hit... it wouldn't be great. But we could play a sound at the position of several hits with a parameter for "how many bullets was that"

--

 ## The theory behind this 
 
Auditory stream segregation
<br>When we can tell sounds apart VS when our brains group sounds together.
* Different factors contribute to how likely we will notice (or segregate) a sound.
	* Rate of change (especially sudden changes)
	* Directionality
	* Volume / Attenuation (distance fade)
	* Pitch
	* Focus of attention
	* Personal differences
	<br/>
	
* Huge area of research, but for this talk we're only ever even considering only a few of the factors on this slide

* Our goal is to play combined sounds only for things that the player would already be grouping together in this way.
   
--

## Moving on

Or... back 15 years

---

# Lets talk about VESSEL

--

## VESSEL

![Video|1000](Vessel-gameplay-watersounds.mp4)

* I worked on Vessel back in 2009.
* Like all games at the time it was a puzzle platformer with a unique mechanic: particle-based liquid physics simulation (and characters)
* Ran on a custom c++ engine with fmod 
* One of my first tasks was finding a way to play reactive sounds for all that liquid.
--

## Types of liquid sounds

We had 3 types of sounds made from clusters of points
* Liquid collision (water hitting solid surfaces)
* Liquid "rushing" (any water particles moving / under sufficient pressure)
* Liquid "fusion" (contact points where two liquids combined to form another, such as lava + water = steam) 

--

## How we approached it
Basically the approach from a few slides back
* Store points where these events occured in lists based on their liquid & sound type combination.
  
* Remove points after a certain time (usually 0.5s)
  
* Cluster (group) points together by proximity
  
* Play sounds from clusters that are close to the player to represent those points

--

## Liquid sound parameters

![[WaterRushingDemo GVYRsihvqVI.mp4|1000]]
* Clusters would send and constantly update audio parameters based on their points
  
* Each sound type was parameterized with
	* a "drop_count" parameter for the number of points in the cluster 
	* several unique parameters for each sound type, using averaged values from the cluster's points


--

## Moving on
Let's give this method a name and go over how it works

---
# Worldspace Clustering

--

### What worldspace clustering is/isn't good for

> [!check] Good for
> Sounds in 2D games 
> 
> Short "impact" sounds, when there could be a lot of impacts
<!-- element style="font-size:75%;text-align:left;width:90%"-->

> [!fail] Bad for
> Perspective 3D games (needs far too many clusters to play sounds distant to the listener)
> 
> Avoiding noticeable spatialization issues (If listener is very close to the cluster and can tell it's coming from a specific spot)
<!-- element style="font-size:75%;text-align:left;width:90%"-->


--
###  Sound design considerations
Clusterable sounds seem to mostly fall into two categories

* Continuous/Looping sounds where fading in/out instances of the sound at different playback times isn't noticeable.

* Short "impact" sounds where lots of the same impact event happen close to one another at almost the same time.
	

--

### Sound design considerations - Continuous Sounds

* These sounds spatialize really well.

* They also benefit greatly from having additional sound parameters based on player observable or influenceable behaviour.
  	

--

### Sound design considerations - Impact sounds

* In these cases we store short lived points where impact events occurred, and use clustering to merge multiple small "hits" into one bigger one.

* Doesn't show off spatialization as well as the looping sounds, but it does handle large numbers of impacts better than limiting voice count, or cooldown timers.
  
* These sounds have a few complexities though
	* They need to use custom clusters that play a new sound if points are added, with a configurable minimum time between starting sounds
	  
	* They don't work well with sounds that have a very short attack & sustain... though if you delay the start of the sound by a frame it can make up for this.

--

### The method

Time to jump into how this works, <br>
It's going to get complicated for a bit.

--

### The method: Lets define some terms 1

* Cluster type:<br> Definition & settings for a kind of parameterized sound which can take a "point count" parameter.
 <br><br>
* Point: <br>A position (and other data) we want to play a cluster type's sound from.  
	* Can be assigned to a single cluster at a time.
<br><br>
 * Source:<br> An object in the game that provides points. Also provides a bounding volume that those points will be inside
<br><br> 
* Cluster:<br> A dynamically spawned sound emitter, representing multiple 'points'. 
	* Always located at the average position of the points assigned to it.
<br><br>	  
* Capture Radius:<br> The maximum distance a point can be from the cluster without being unassigned from it.

--

### The method: Lets define some terms 2

 <br><br>
* Spatialization:<br>Binaural directionality for positional sounds (as opposed to mono or stereo pan). 
 <br><br>
* Listener:<br>The position & rotation that audio direction is relative to (usually the camera) 
 <br><br>
* Attenuation origin:<br>The position attenuation (distance falloff) is relative to. 
	* Usually same as the listener position first person
	* Usually the player character/camera focus in 3rd person
	* AKA "Distance probe" in Wwise / "Attenuation override" in Unreal 
	* Not provided by unity's in-built audio, but you can add it pretty easily

--

###  The method - Capture Radius

In this method a cluster's "capture radius" is relative to the number of points assigned to the cluster:  <!-- element style="text-align:justify;"-->

* it increases with more points, shrinks with less points
<br><br>  
* there is a configurable minimum radius
  <br>(otherwise each cluster would likely only contain a single point)
<br><br>
* there is a maximum capture radius,
  <br>(to avoid runaway clustering)

--

###  The method - 1-3

> [!example] 1. Re-position clusters with changed points
>  This is to ensure clusters of moved points are in the correct locations before we assign points to clusters
<!-- element style="font-size:75%;text-align:left;"-->


> [!example] 2. Unassign any points that are outside the "capture radius" of the cluster they are assigned to.
> This covers cases where the cluster or point has moved and is no longer close enough to belong to it's current cluster
<!-- element style="font-size:75%;text-align:left"-->

> [!example] 3. Assign points to existing clusters they are within the "capture radius" of, or create new clusters to assign them to if that fails.
> This creates any needed clusters and ensures as many points are clustered as possible, but we may still have overlapping clusters
<!-- element style="font-size:75%;text-align:left"-->

--

###  The method - 4-6

> [!example] 4. Merge smaller clusters into larger clusters who's "capture radius" fully overlaps all their points.
> This eliminates clusters that are completely overlapped. I experimented with merging any overlapped point but results weren't better and it added a lot of complexity
<!-- element style="font-size:75%;text-align:left"-->

> [!example] 5. Fade out sound for clusters that have no points, and destroy any clusters with no points that have fully faded out.
> This is how clusters are removed
<!-- element style="font-size:75%;text-align:left"-->

> [!example] 6. "Refresh" each changed cluster: starting the cluster's sound emitter and moving it to the the cluster's position, setting "point count" audio parameter as well as any custom ones.
> This is when the cluster interprets data from its points and communicates this to the audio engine.
<!-- element style="font-size:75%;text-align:left"-->
--

## The method - pseudocode
:::<!-- element style="font-size:200%;text-align:center "-->
<!-- slide style="font-size:50%;text-align:left"-->
> [!info] Not covering this during the talk
> This is here so you can refer to it later if you need to
<br>

#### When assigning/unassigning points from a cluster:
1. Move the cluster to the average position of it's assigned points
2. If the cluster's position was moved, mark all it's assigned points as changed.
3. Update the cluster's "capture radius"
4. Mark the cluster as changed
<br><br>
#### On an update when any points have changed:
1. Loop through all clusters that have changed or removed points
	1. Move the cluster to the average position of it's assigned points
	2. If the cluster's position was moved, mark all it's assigned points as changed.
	3. Update the cluster's "capture radius"
	4. Mark the cluster as changed
<br><br>
2. Loop through all changed points
	1. If the point is assigned to a cluster and is outside the cluster's "capture radius": unassign it from the cluster.
	2. If the point is unassigned: search for an an existing cluster who's "capture radius" contains the point, if found assign the point to that cluster.
	3. If the point is still unassigned: try to spawn a new cluster, if successful assign the point to the new cluster.
	4. Finally: If the point is assigned, mark it as unchanged
<br><br>
3. Loop through all changed clusters: 
	1. Find any clusters with less points who's points all are within this cluster's capture radius and reassign those points to this cluster.
  <br><br>
4. Loop through all changed clusters:
	1. If the cluster has no points, remove it.
	2. Start the cluster's sound emitter if it's not already playing
	3. Update the sound emitter with the new position and "number of points" parameter (along with any other custom parameters)



--

## Moving on

Let's to move onto the new stuff

---

# Perspective clustering 

--

### What perspective clustering is/isn't good for

> [!check] Good for
> 3D games (with perspective projection)
> 
> Automatic & seamless transition of far ambience to near (or per object) ambience. 
> 
> Point cloud defined volumetric sounds (eg, particle effects)
<!-- element style="font-size:75%;text-align:left;width:90%"-->

> [!fail] Bad for
> Higher performance overhead than worldspace clustering
> 
> Implementation time & complexity
<!-- element style="font-size:75%;text-align:left;width:90%"-->


--

###  Basing Capture radius on auditory stream segregation
:::<!-- element style="font-size:150%;text-align:center "-->
<!-- slide style="font-size:75%;text-align:left"-->

![video|1200](clusters_distance_based_capture_radius.mp4)
:::<!-- element style="text-align:center "-->

* We basically want to cluster sounds as much as we can *without* the player noticing, so it makes sense to base the method on stream segregation factors.
  
* Just considering Directionality and Attenuation as noticeable factors for now.
	* We don't want the angle between the cluster and any of it's points from the listener's perspective getting large enough that the player would notice.
	  
	* Similarly we don't want the distance from the cluster to a point to be large enough that the attenuation of the cluster's sound is noticeably incorrect.

--

###  Distance based capture radius

 * We want the "capture radius" to be based on this "maximum unnoticeable error angle" from the perspective of the listener (or attenuation origin, whichever is closest). 

* This turns out to be the simple formula: 
	```
	CaptureRadius = MinDistance * MaxNoticableListenerSinRatio
	```


* Where this is the sin ratio constant
	```
	MaxNoticableListenerSinRatio = sin(MaxNoticableListenerAngle) 
	```

* This turns out to be a surprisingly useful representation for lots of settings in this method. Many things are distance relative and "angle at a distance" is a quite intuitive way to think about these.

--

## Minimum capture radius for Attenuation origin
* One small but very helpful tweak I found it was to clamp distance the attenuation origin to a minimum value when calculating capture radius. 
<br><br>
* This means the capture radius will:
	* shrink to a minimum value if the Attenuation origin (3rd person character) approaches the cluster.
	* still shrink to zero as the Listener (camera) approaches the cluster
<br><br>
* This prevents clusters getting too small (and therefore using more clusters) in a case where there really isn't a benefit to doing so.
--

## Changes to the previous method

In this method a cluster's "capture radius" is a factor of distance from the listener/attenuation origin.

The method is almost identical, however we'll need to add several new features once we've updated the method in order to handle all the edge cases of this change.

--

###  The method - 1-4

> [!quote] 1. Re-position and update "capture radius" of clusters with changed points
>  This is to ensure clusters of moved points are in the correct locations before we assign points to clusters
<!-- element style="font-size:75%;text-align:left;"-->


 > [!example] 2. [NEW STEP!] Update "capture radius" of clusters if they have moved, or if the minimum distance to the listener/attenuation origin has changed.
> This is to ensure clusters of moved points are in the correct locations before we assign points to clusters
<!-- element style="font-size:75%;text-align:left;"-->

> [!quote] 3. Unassign any points that are outside the "capture radius" of the cluster they are assigned to.
> This covers cases where the cluster or point has moved and is no longer close enough to belong to it's current cluster
<!-- element style="font-size:75%;text-align:left"-->

> [!quote] 4. Assign points to existing clusters they are within the "capture radius" of, or create new clusters to assign them to if that fails.
> This creates any needed clusters and ensures as many points are clustered as possible, but we may still have overlapping clusters
<!-- element style="font-size:75%;text-align:left"-->

--

###  The method - 5-7

> [!quote] 5. Merge smaller clusters into larger clusters who's "capture radius" fully overlaps all their points.
> This eliminates clusters that are completely overlapped. I experimented with merging any overlapped point but results weren't better and it added a lot of complexity
<!-- element style="font-size:75%;text-align:left"-->

> [!quote] 6. Fade out sound for clusters that have no points, and destroy any clusters with no points that have fully faded out.
> This is how clusters are removed
<!-- element style="font-size:75%;text-align:left"-->

> [!quote] 7. "Refresh" each changed cluster: starting the cluster's sound emitter and moving it to the the cluster's position, setting "point count" audio parameter as well as any custom ones.
> This is when the cluster interprets data from its points and communicates this to the audio engine.
<!-- element style="font-size:75%;text-align:left"-->
--

## Moving On... What else is needed?

Unfortunately the cluster radius change adds a lot of new edge cases.<br>We'll need to add these feature to resolve them. 

> [!Example] Point weight interpolation
> Sounds teleporting their location happens lot more now and will be very noticable.
> 
> So we need a way to interpolate out the direction & volume jumps caused by reassigning points between clusters. 
<!-- element style="font-size:75%;text-align:left"-->

> [!Example] Culling
> It's easier to have large numbers of points in 3D worlds, so we need to efficiently distance cull points without the player noticing.
> 
> We also need a way to ensure that if we hit our limit of clusters for any sound type, those clusters are used for sounds close to the player.
<!-- element style="font-size:75%;text-align:left"-->


> [!Example] Custom point data / Custom audio parameters
 > Finally as this will get quite complicated, we can't rebuild this system for each new type of sound that has different parameters. It needs to be able to take custom per-point data and turn that into custom per-sound behaviour & audio parameters.
<!-- element style="font-size:75%;text-align:left"-->
 
---

# Point-Weight based Interpolation 

--


## Interpolation?

![[clusters_interpolation.mp4]]
:::<!-- element style="font-size:150%;text-align:center "-->
<!-- slide style="font-size:75%;text-align:left"-->

* As mentioned before, sudden changes are very noticeable. So having the position of a cluster's sound teleport to the average position of its points whenever points are added or removed is something we want to avoid. 

* The same goes for the point count and any other sound parameters the cluster is sending to the sound engine.


* Initially I tried to work around this by directly blending cluster positions / values, then I tried fading out clusters that changed significantly. Neither worked well at all.
--

## Point-Weight Interpolation?

* The best solution I found was to have points between clusters transition between clusters over time. This works by each point having a "weight" percentage that can be split between multiple clusters. 

* Over time each point will increase its weight for the cluster it is assigned to until that weight reaches 100%, in doing so reducing weights for any other clusters until they become 0% and are removed.
  
* If a point is being culled (and not immediately), we just reduce all weights to 0% in this way. Once all weights are zero, the point's data can be safely archived (to be un-culled later) or deleted without the player noticing.

--

## How does this affect clusters?

* Clusters keep track of all "weight points" that have weight values with them. These are used for almost everything we send from the cluster to the audio enigne.

* The cluster's position doesn't change, the weighted average position of these "weight points" is used as the audio emitter position for the cluster. 

--

## Nice side effects

> [!Example] Automatic interpolation for "Number of points" audio parameter
> We can also use the total weight from all "weight points" as our "number of points" audio parameter. 
<!-- element style="font-size:75%;text-align:left;width=90%"-->


> [!Example] Easy to blend custom audio parameters
> Similarly most custom audio parameters from points are best handled by using the weighted average value from each "weight point".
<!-- element style="font-size:75%;text-align:left;width=90%"-->

--

## Moving on... 

We still need to cull points


---

# Culling

--

## Culling?

![](clusters_culling.mp4)
:::<!-- element style="font-size:150%;text-align:center "-->
<!-- slide style="font-size:75%;text-align:left"-->

* We only have a limited number of clusters for each sound type, so we need to ensure they're not too far away to hear. So we cull points that are too far away from the attenuation origin.
  
* To handle this each cluster type defines a maximum culling distance (usually equal to the maximum attenuation distance of the sound).

* We also want to ensure that if we don't have enough clusters for all objects around us, we prioritize using them on the closest ones to the attenuation origin. 
  
--

## Culling!

* In order to cull points without checking the distance to each point individually, we perform culling by the "source objects" that provide points first.

* If  a source's bounds are:
	 * Outside the culling distance: all the source's points are removed.
	 * Overlapping the culling distance: each point from the source is checked against the culling distance individually
	 * Fully inside the culling distance: none of the points are culled.

--

## Interpolating culling

 * As mentioned earlier, we interpolate points using weights to avoid noticeable changes. And  culling is no exception to this. 
 
 * This is done by removing culled points from clusters, but still allowing culled points that have weights for clusters to interpolate those until the point is fully blended out.
   
 * Once blended out, the data for culled points is not deleted, instead it's just "culled" which prevents it being used. 

--

## Dynamic culling distance

![video|1000](clusters_dynamic_culling_distance.mp4)

* The other thing we need from culling is to allow us to prioritize using clusters close to the attenuation origin if we don't have enough clusters for all nearby points.

* To do this we simply remove the most distant cluster entirely by reducing the culling distance if we run out of clusters during an update.
  
* Then we only gradually increase the culling distance towards it's default if we are sufficiently below the "max clusters" limit for this cluster type (at least 2). 

* Despite the simplicity, this method seems to work incredibly thanks to the point-weight interpolation of culled points / clusters.  

--

## Moving on... 

We still need to support custom point types so we can use custom audio parameters!

---

# Custom point data / Custom audio parameters


--

## Custom point data / Custom audio parameters
:::<!-- element style="font-size:150%;text-align:center "-->
<!-- slide style="font-size:75%;text-align:left"-->

![[clusters_custom_points_clusters.mp4]]
* To get good results, the system needs to allow the use of additional custom sound parameters on a per-cluster type basis.
  
* To do this, the system needs to support custom point data as inputs, and custom clusters that  can turn that into custom sound parameters.
  
* In the video above the cows are using a custom "speed" parameter based on movement, and a "agitation" parameter based on how much the player has chased them recently

--

## Implementing custom point data
> [!info] Code implementation is beyond the scope of this talk
> Going light on this can be a full programming talk on it's own 
<!-- element style="font-size:50%;text-align:left;width:90%"-->


> [!exmple] For C#/Unity
> My preferred approach is to abuse generics.
> (My gcap 2022 talk on c# generics covered this at length) 
> 
> Just be aware this is not done particularly well the the example source due to time constraints
<!-- element style="font-size:75%;text-align:left;width:90%"-->

> [!exmple] For Unreal
> I recommend using instanced structs for custom point userdata, <br>
> but also suggest keeping userdata seperate from the point data structure used by the clustering system.
<!-- element style="font-size:75%;text-align:left;width:90%"-->

--

##  Live demo
> [!info] Will do this if there's time
> A more detailed look at the what's in the video on the last slide
<!-- element style="font-size:75%;text-align:left;width=90%"-->

--

## Moving on

There's quite a lot of technical stuff we can cover if there is time.

---

## Technical details
> [!info] Included for reference
> Will cover this in the talk if there's time (and if folks want me to)
<!-- element style="font-size:50%;text-align:left;width:60%"-->

--

### System parts & Structure
:::<!-- element style="font-size:200%;text-align:center "-->
<!-- slide style="font-size:50%;text-align:left"-->


* Sources
	* An object that provides point data to the system. 
	* Also provides the cluster type the points relate to, and culling bounds that all it's points will be inside.

* Point Data
	* Structs that contain the actual per-point data. 
	* This can either be a single struct with all the data (easier to do in c#) or split into a standard "PointData" struct and a custom "UserData" struct (easier to do in unreal).
	* Contains 
		* point Id number (unique to other points from the same source)
		* world position
		* optionally: group Id number (we'll cover this later)

* Cluster Manager 
	* Owns the system and all its data
	* Provides access to external code
	 * Manages and updates cluster type containers for each cluster type that is in use.
  
* Cluster Type Container
	* Owns everything relating to a cluster cluster type, owned and updated by the cluster manager. 
	* Different cluster type containers have no interaction with one another.
	* Also acts as the pool for spawning/despawning clusters of this type.

* Optional: Cluster Type GroupId Container 
	* Owns clusters and points with specific group id number.
	* Different group containers have no interaction with each other.
	* Points can move from one group to another by changing their group id number.

--

## Optimizations 1-2
:::<!-- element style="font-size:120%;text-align:center "-->
<!-- slide style="font-size:60%;text-align:left"-->

### Optimization - Buffered update sequence
* One of the best ways to avoid clustering updates taking too long on any one frame is just to restrict how many things get updated on each frame.
  
* Instead of updating everything that can be updated each frame, it's not difficult to store "buffers" of things that are ready for each update stage. 

*  There's always the risk that this will allow cluster sounds to end up noticeably distant from where they should be visually... however this will be far less noticeable than the game dropping frames.
<br><br><br><br>
### Incremental averaging
* Each time we add or remove a point from a cluster, the cluster position must update to the average position of all it's points. 
  
* Normally this is done by adding up the position of all points, then dividing by the number of points. However with very large numbers of points this can become a performance problem.
  
* We can instead use weighted averages to instead just add or subtract one point's position from the cluster's current position. This doesn't need to loop through all points so it's much faster.
	* To update the average position for a moved point, we just weighted average subtract the point's previous position from the cluster's position, then weighted average add the point's new position.
	  
* The downside to this is that using incremental averages have floating point error that increases with the number of points in a cluster. But it's just something that can be handled in code by being aware of it (especially when comparing distances).

--

## Optimizations 3-4
:::<!-- element style="font-size:190%;text-align:center "-->
<!-- slide style="font-size:45%;text-align:left"-->


### Lazy calculation of points radius
* Similar to incremental averaging, we can optimize a lot of the process by remembering the distance from each cluster to the most distant point assigned to it, which we'll call the "points radius" (as all the cluster's points are within this radius).
  
* In the demo this shows as a white circle within each cluster (though it won't be visible unless there are two or more points at different positions)
  
* It's very useful to prevent needing to loop through all points in a cluster when checking if a cluster's points are fully overlapped when merging, or if we are allowed to add a point to a cluster.
  
* It also has the issue that it potentially needs to be recalculated by looping through all points in a cluster whenever points have been added or removed, which happens enough to be a performance issue if there are enough points. 
  
* But we can sidestep this cost in a few ways.
	* Firstly: we only calculate the points radius by looping if we have to, and we remember the current value.
	  
	* Second: if a point is added or removed to a cluster and is less distant than the current points radius, the points radius is unchanged.
	  
	* Third: if a point is added to a cluster or moved and is now more distant than the current points radius, the points radius is now the distance to the new point. 
	  
	* Fourth: if a point was moved closer to the cluster or was removed it and it's previous position was the points radius, we don't know the current point's radius... but we know it's less than the previous one, so we store it as a negative number to indicate it's "something less than this value". 
		  (in the demo this shows up as a magenta circle instead of a white one)

* Using these rules it's possible to avoid recalculating the points radius using looping almost entirely, mostly by making a few functions to handle comparing numbers to the points radius.
<br><br><br>
### Spatial storage of clusters 
* An optimization not present in the demo is to reduce number of distance comparisons between clusters, or points and clusters by storing clusters in a way that allows us to only loop through ones close to the point or cluster we are comparing them to.
  
* Good candidates for this are a spatial hash grid, or octree.
  
* This allows us to cut down a lot of unnecessary looping when assigning points to clusters. And to a lesser extent also reducing the number of loops when checking if clusters can be merged.
  
* There however isn't much point in storing points in this way. We don't ever search for points within an area, and there is a not insigificant cost to adding/removing/moving objects stored in one of these.

--

## Moving on

Damn that was a lot of words

---

# Debrief / Final Q&A

Thanks for listening!

> [!error] Special thanks to Ella Van Dyck
> Iterated on this system with me and also provided the sounds used in this demo
<!-- element style="font-size:50%;text-align:left;width:60%"-->


> [!error] Honest Plug
> Gameloft Brisbane is a pretty awesome place. 
> 
> Projects are ambitious, the people are great, legitimately good studio culture. <br>
> In 20yrs in games I've only said this about 2 large scale studios.
> 
> Hit up Jefferson Blinco if that's something that interests you
> (jefferson.blinco@gameloft.com)
<!-- element style="font-size:50%;text-align:left;width:60%"-->

