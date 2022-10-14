# New Dawn

Basically a drone control ship.

## Physical Design

Based off of the Shadow Broker's base.

## Drones

The drones are called Starlets/Starlet drones.

## Milestones
- Drones that can undock, navigate to any arbitrary point, and redock to the New Dawn, all while avoiding colliding with the New Dawn, asteroids, and each other, if possible.

## Long-term Goals

- Drones and ship working both in space, and in gravity well and atmosphere.

## Ideas

-  The drones should eventually have interchangeable tool heads that they can coordinate with the ship facilities to exchange automatically.
- The drones' software can't be easily broadcast and updated? So need a version field in communications, or capability reporting? Overkill.
-  How to update all required computers? Just not have many on the New Dawn? Drone control should probably be separate from other ship functions.
- Laser antennas vs. antennas, do we want the New Dawn to be able to be stealthy (both in view and friendlies picking up broadcasts)?
- Drone collision detection using camera raycast or sensors or both?
- When drones are taken control of manually, eg. through remote control, they should report this to the mothership which can display that and who is controlling it.
- Drone program to latch onto other ships and push them into crashing on a planet.
- Drones should be able to be highly independent, eg. sent to mine somewhere far away like suggested in https://www.youtube.com/watch?v=U_iWjOl10-U
- Inspiration from Craig Perko's Let's Break Space Engineers, eg. https://www.youtube.com/watch?v=6RG9cpuHkWI
    - That one is about reacquiring laser antennas.
    - Full demo: https://www.youtube.com/watch?v=fIwxhF_w80Q
    - https://www.youtube.com/watch?v=5e9Agq3teOY
- Drones keep track of where the mothership's last reported position is so they can go back to it.
- Flight control: getting up to speed and cruising, eg. https://www.youtube.com/watch?v=fIwxhF_w80Q
    - Useful for other ships too.
- Docking: move to point a little bit away from connector, re-orient, go back until hit connector, lock.
-   The New Dawn can on startup check for Starlets, and offer to build more if slots aren't filled. It can also reinitialize their names based on the slot they're in.
-   Specialized large ship antenna-boost drones? Laser antennas? Handoff depending on situation?
-   Building more drones from merge blocks, connect to connector, then delink merge block?
-   New Dawn asteroid database - the mothership keeps track of asteroids drones (and the ship itself) encounter so future drones can plot around them.
 