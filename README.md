# Milestone 1
The CameraController Visual Scripting Graph controls the motion of the camera. It monitors the Unity PlayerInput component. As the player presses certain buttons, the PlayerInput component sends a Unity event and passes it to the CameraController Graph. On the activation of the event, the CameraController Graph moves the MainCamera object. 

1. More specific details are added to the chart, and it now shows the flow of the interaction between objects and scripts. The logic of the auto-generation of maps is added; the player input logic is added; the unit control logic is added.
2. I used a finite state machine for the console, the public enum DebugMode. The player uses the console to toggle DebugMode between the state of On and Off. There are if() statements in the methods of multiple scripts. If the state of DebugMode is On, more detailed information will be printed by Debug.Log() as these methods are used, so that the developer (which is me) will be able to get the debug for further development when and only when needed. 

# Milestone 2
## Q1
I will need to build the features of movements of units. 
1. Add Map-to-world/world-to-map position calculator that can be used to assist input and visual output by converting between the world position that indicates the object's position on the screen and the map position that indicates the position on the hexigonal map on which all the data and game logic are based.
2. Build the unit data framework that indicates some basic properties and movement features of the unit.
3. Build the selection function, which includes:
    a. Mouse-click checker that uses the Map-to-world/world-to-map position calculator to check mouse click (if it is on UI, selectable objects or not, which type if it is)
    b. Selection manager that can trigger that realized the function of selecting/deselecting units (and potentially other objects going to be added)
    c. Selectable tag script that indicates that the object can trigger selection and indicates its type
4. Build the movement function, which includes:
    a. Direction finder that can find all surrounding tiles and their data given a Vector2DInt map position
    b. Path calculator that can calculate the shortest path from one point to another so that it can calculate all reachable tiles for a unit and the paths to reach them. 
    c. Update mouse-click checker.
    d. Add movement point cost property to tile data.
5. Build the movement animation framework, which includes:
    a. An animation manager that makes the unit moves through the pre-calculated path at a given speed.
    b. Make the animation manager allow using an animator for unit game object.
    c. Art assets for animation (if time permits)
6. Build the turn switching framework, which includes:
    a. Turn manager that allows the switching of turns to renew part of the game data
    b. Button object that allows the players to switch the turn witout having to toggle the console
    c. Same-screen multiplayer rotation framework
7. Add a testing initializer that asks the map generator to generates a random map of a proper size with a random seed and spawn the player on a random tile that is not a sea. It initializes the number of turns.
8. Bug fixations for the map generation system and the movement of the camera.
## Q2
It does not really help because I have never made games like this (and it was never taught). Therefore, I need to spend a lot of time exploring and trying the code structure myself. The very first breakdown that I came up with was too vague and general to be practical. The way that I will improve it is to make it exactly like the one that I wrote for Q1, which ic specific enough. 
## Q3
![My graph](image.png)
![My code 1](image-1.png)
![My code 2](image-2.png)
I used a custom event to trigger the event and pass the data from my code to my graph. The event trigger is in Console.cs, and the event is monitored by CamaraController. Its purpose is to make me able to set the movement speed and the zoom speed of the camera on the console. 
## Q4
The finite state machine. 
![SelectionManager.cs](image-3.png)
It is a state machine that indicates if something and what type of object (if true) is selected.
![Console.cs](image-4.png)
It is a state machine that controls the debug log. If it is on, more information will be logged.
![Unit.cs](image-5.png)
It is a state machine that indicates the what the unit is doing, which will be used to control the animation.