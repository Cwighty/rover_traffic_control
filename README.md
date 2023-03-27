# rover_traffic_control

## Things That I Was Proud of With This Project:
- Swarm Control Of Rovers/Helis
  - Shared concurrent view of incoming map data from all rovers/helis
  - Multi-Heli flight formations (discontinued)
  - Multi-Rover routing to win
- Pathfinding: 
  - getting it to even work was really hard
  - overcoming the algorthim not being able to find a path with so many cells by trying many different variations of giving the algo a partitioned section of the map
  - incentivizes driving straight when battery is fine
- Map Caching:
  - Hashing the low res map, to get a unique file name for each map
  - If the map has been cached, load that in, don't waste time scouting with helis
  - if the map isnt cached, scout with heli, when rover wins, send more helis to cache the whole map
    - Breaking up the map into different sections, distributing those sections to the nearest helis on standby
- Ordering the targets:
  - Permute the target orders, find route with minimal distance
  - Find all the closest points on the edge to any target, include that in the calculation to find the best starting spot based on traveling distance
  - Spawning until suffeciently close to that best starting point 
- Azure
  - Running on azure vm local to server
  - Ability to up cpu for competition 
  - Avoid network congestion from competition room?
  
