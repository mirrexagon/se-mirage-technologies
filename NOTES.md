Make translation control mode enum: none, velocity, position

If a direction's thrust is 0 does stuff mess up?

Redirect Echo to LCD: https://github.com/malware-dev/MDK-SE/wiki/Debugging-Your-Scripts#echo-performance-and-tricks

Camera raycast to detect obstacles. You can get bounding box of detected object

---

New Dawn docking system:
Drone requests docking and transmits its position, or New Dawn tells drone it wants it to dock and asks drone for its position
New Dawn checks which connectors are free and closest to the drone, and assigns it. Transmits the location and orientation of the port to the drone
Drone used this information to maneuver and dock

---

Grid-to-grid communication for drone commands: https://github.com/malware-dev/MDK-SE/wiki/Antenna-Communication-(IGC)

---

Given an orientation matrix that rotates a body from zero rotation to its world orientation, it converts from the body frame to the world frame. It's how you get from a local frame to the world frame.

---

https://gfycat.com/UncomfortableSinfulHorse

New Dawn/Harmony

---

Drone AI:

Have a planning system on top of a runner that executes each command (change orientation, move to, etc.) - commands have end conditions, can be aborted if planner needs to replan based on new information (eg. about to collide, new remote command)

---

Warn if thrust gets too low and ship isn't responding to match, eg. when on planet and too high for atmospheric thrusters
warn about things like not enough fuel to perform projected sequence
