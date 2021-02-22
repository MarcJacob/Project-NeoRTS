# Project-NeoRTS

## Streamed Online RTS Project.

### Link to current Client build : https://drive.google.com/file/d/14_UJcRmgkqBtBGLjMqTX6O8486oE9Dlb/view?usp=sharing

Note that the game isn't in a playable state : all present interactive elements are built as a test, using the Framework beneath as a service of sorts. 
Development was entirely streamed on my Twitch channel : https://www.twitch.tv/Hoshiqua
... and entirely uploaded to my Youtube channel : https://www.youtube.com/channel/UCvdyDp5GVFhEls_3hy7M38A

Because of changes to my stream schedule and format, development is for now suspended. Video presentation of the current codebase coming soon.


## Understanding the code base (Pt 1 : "Application" projects)

The Neo RTS framework is my attempt at building my very own Online RTS game in the cleanest way I could achieve, without using any third party libraries other than Unity itself, and only for the Client (no Unity code is strictly speaking used outside the Client code base, although they might still have a reference to it that I forgot to remove).

The only language used across the entirety of the code (to my regret, see the last section) is C#, including so-called "unsafe" code. All network communications go through my custom made network solution that relies only on the C# socket API, for now using only TCP.

The code is split into three different projects : the *Client*, the *Server* and the *Commons*.

### Client

The actual Unity project under the main NeoRTS folder. The code can be found under NeoRTS/Assets/Sources.

The *Client* is what the actual player needs to play the game both online and in singleplayer. It includes the *Commons* but **NOT** the Server code. It features mostly code to graphically "represent" a game's state (through the *Pawn* system), UI code (through the *UI Module* system), a global State Machine to drive the user experience (mostly used for major menu navigation), connectivity to a Server, the ability of starting a locally ran, singleplayer game independent of any connection to a server, and finally on the Editor side some tool code to create & edit the *Game Data*. 

The *Client* is the only one of the three Project that directly relies on and runs as a Unity application.

### Server

Secondary console application project, can be found within NeoRTS/NeoRTSServer.

The *Server* is, in and of itself, just a console application able to listen for connection requests on a given port and provide services to Clients that authenticate themselves in order to access various services. It includes the *Commons* but **NOT** the Client code.

Once a connection is established, it expects to receive an Authentication message that, as of now, simply contains the Player's name and serves as a way to confirm that this connection *Channel* does indeed link to a *Player*. A special *Manager* creates a *ConnectedPlayer* object in reaction to that, which is essentially an interface object that allows the rest of the server's services to interact with that player in a simpler, more stable way (for example, although it is not done yet, it would be easy to insulate the server's systems from a sudden disconnect / reconnect event where it wouldn't be worth completely "unloading" the player. Think of "drop time" a lot of online games have).

The *ConnectedPlayer* keeps track of network *Messages* sent through its current *Connection Channel* and is then able to send / receive requests for :
- Matchmaking
- Joining and leaving chat, sending a message
- Anything else we might want to make, as the way it is implemented makes it really easy to iterate with new features.

## Understanding the code base (Pt 2 : *Common* code)

This project deserves its own part because it contains the majority of the framework's code and complexity. It is directly, and extensively used by the *Client* and *Server* code. It can be found in NeoRTS/NeoRTSCommons.

In spirit, the *Common code* or *Commons* is / are used as a service by the *Server* and *Client*. It has a variety of features ranging from the full *Simulation code* to run an actual gameplay session of our game, to utility code like common string encoding, mostly for sending chat messages over the network. That, amongst much more. When compiling the Commons project, a C# Framework DLL is built and automatically deployed to the relevant dependencies folders on the *Client* and *Server* codebases.

Since the *Commons* code base is relatively extensive compared to the other two, I will go through every major element that seem worth explaining, and describe their role as well as the thinking behind them.

### Communications

Building an Online game, you are better off developing with the Clients / Server(s) duality in mind from the start. "Converting" code from Singleplayer to Multiplayer, whatever that means, ranges from headache-inducing to straight up impossible. This means the more you wait before putting your (and your teammate(s)'(s)) mind(s) in the paradigm of building a multiplayer game that will at some point have to rely on data synchronization and security, the more work you create for yourself down the road. And that's more of an exponential relationship than a linear one...

As such, my very first priority when starting the project out was to make it exactly so : build the code base around the reality of (potentially) having to communicate with *something* that isn't the app itself (*...at least not always, clarified later*) to run the game. Yet, I didn't want to tangle with *actual* Networking code right away.

Introducing: the **Communication Channel** object / layer of abstraction.

A *Communication Channel* object **class** defines the ability of creating a channel making use of a specific communication protocol, usually provided with the information it needs to find its interlocutor (the rule one channel object = one interlocutor isn't strictly enforced but it is what's expected). An **instance** is thus a "linked up" object that is ready to send and receive message with a specific interlocutor, using a specific protocol. All communications use the *MESSAGE* data structure, which is basically a fancy wrapper around an array of bytes, the first 4 of which being a **header** that defines the Message's type and purpose.

Although currently there is such a *Channel* type that uses a TCP connection and is thus network-capable, it did not come along until relatively late in development. For about a third of the total development time (~130 hours as of writing this), not a single line of networking code existed, and instead the client & server existed both within the same application (back before the code was divided into three different projects). To communicate, they simply used a *LocalAppCommunicationChannel* which had a very simple "protocol" : be linked with another such channel before usage, and then simply call the "paired" channel's Receive() function when sending a *Message* through.

It was crude, but it worked: the transition to networked communications was almost entirely seamless, and a surprisingly small percentage of the code base was really affected.

Nowadays, the *Client* even still uses that simple *Channel* type, and it's paired with... *itself*. And there's two of these channels. The reason for that will be explained in the Gameplay / Simulation section.

In order to create and decode *MESSAGE* data structures, a whole other layer of abstraction called *Message Packers* is used. *Message Packers* define an encoding & decoding scheme from a data structure of a certain "format" (simplest "format" being *unmanaged* structures that can all just be directly reduced to a single sequence in memory) to a byte array (returned as a *MESSAGE* to include meta data such as the **Header**) and back.

A single *Packer* type defines an entire encoding / decoding *scheme*, so it is not necessarily on a one-to-one relationship with a certain message *Header/Type* or with a certain data structure to be transferred. Some are, others like the *SimpleMessagePacker* are used extensively for a wide variety of purposes as it is able to work with any data structure deemed to be *unmanaged* (IE no direct & indirect underlying pointers or references are needed to implement it).

In usage, the Server and Client both maintain a globally accessible set of channels that are identifiable through a unique ID (which the channel objects themselves are aware of). This is done through a *ChannelContainer* object. In the case of the Server, the number of active channels varies a lot, whereas the Client has a maximum of three (the "Self" channel, the "Server" channel and the "SP Match" channel).

### Managers

Throughout the entire code base, there's **extensive** use of *Manager* objects. A *Manager* defines a single or multiple behavior over time and / or in reaction to events that require persistent data. In this codebase, every manager has the possibility of overriding a set of functions:

- Start, Update and End. Pretty self-explanatory: whoever "owns" the *Manager* calls these to start, update (given a deltatime) and "end" (IE deinitialize) the State.
- RegisterMessageReception & UnregisterMessageReception : These have to do with the *Message Dispatching* system. In the code base there exists an extensively used *MessageDispatcher* object type whose role is to store a list of Registered *Message Handlers* that it calls, passing in the *MESSAGE* structure, depending on the kind of registration that handler went for (*Header registration* = React to Messages of a certain Header / range of headers, *All* = React to all Messages, *Channel* = React to all Messages whose metadata indicate they were received on a specific Channel ID). Because *Managers* must NOT know about their owner directly (IE Direct reference to what owns them are forbidden for greatest flexibility) the way they are allowed to react to messages is by being allowed to register and unregister themselves on a Dispatcher object that whatever owns them passes to them using this function.

What *Managers* do around the code base is extremely diverse and there are many layers : *Managers* can be found on the highest level (owned by the global management class like GameClient) just as well as on much lower levels (Match objects that are themselves usually owned by a manager, have their own Managers !). They're a cornerstone of the "control flow" of the software.

Most of the time *Managers* are not owned directly, but rather through a *ManagerContainer* object so that changing what Managers something contains can be changed just by modifying that thing's construction code rather than its class definition (which I personally consider to be cleaner). When using that container, specific *Manager* objects can be queried using the *GetManager*\<Type\>() function that simply returns the first Manager of that type contained within. If none are found it returns null. This might be the one drawback of this approach : without the specific *Manager* types being present in the class definition, we are not strictly speaking guaranteed that it is, in fact, owned by objects of that class.

### Gameplay

A game using the NeoRTS framework runs its actual "Simulation" gameplay code using a *Match* object. A Match object, upon being fed *Start data* (which, as of now, contains all starting *Objects*), starts simulating a game using a mix of *Managers*, *Workers* and the *Object Memory*. Currently, a Match only ever ends if one of its players disconnects from the server (the match is thus ended externally). It never ends in Singleplayer as for now the only way of leaving the game at that point is to close it entirely: current code is lacking an endogenous end condition.

#### Objects & Memory

In a game ran on the NeoRTS framework, all dynamic elements that are part of the game (think Units, Buildings, Projectiles, Persistent spells, Destructibles...) are interchangeably known as *Entities* or, more commonly (and as in code) : *Objects*.

The problem Objects solve is relatively simple: we need our simulation to update more-or-less independent objects of variable types, data, and behaviors. Especially with a heavily object-oriented language like C#, there are many options. Each have their pros and cons. It is tempting to go for a very simple solution where, for example, you would design each of your broad *Object* types by inheriting from an Object class, or by composition through components that would be centralized by an *Object* instance through a list of references.

That solution would work well in many cases, but not here: I had high ambitions for the performances of the game and wanted to support at least 4 figures of dynamic objects being active in a single match at a time. When you want to go that high, you cannot scuff on any major performance concerns, and the one many solutions do sacrifice is **Cache locality**, or **Avoiding Cache Misses**.

The fact is that we need our data to be tightly packed together in memory so that as much of it as possible can be loaded together into processor caches and thus give a great speed boost to our object processing throughput. This means the data has to be organized in a way that disallows design simplifications and flexibility like the aforementioned object-oriented solutions get.

I thus built a design that prioritizes where the data ends up in memory and speed of reading and processing that data, at the expense of flexibility when necessary. This is known as a *Data Centric* design.

*Objects* can "own" *Data Component*, which are simple, usually very small data structures, often pure data containers with no methods or complex constructor. The actual data is laid out inside DataContainer objects, which are simply a wrapper around an array of a specific type of data structure and are able to react to *DataChangeEvents* (Explained later). All *DataContainers* are centralized within the *ObjectMemoryManager*, one instance of which every *Match* object owns.

In order to assign that data to actual *Objects* and generally manage it, *Object* instances turned out to be simple lookup tables. Each *Object* contains two arrays of constant size of type *uint*. The first contains the *Type ID* (*which is also the ID of the actual container*) of an owned component (*which gets calculated through reflection in alphabetical order, so as to maintain consistency across machines given they have the same version of the codebase*), and the second contains the corresponding index within the *DataContainer* of the actual type...

Creating a new type of data is extremely easy : access the *Commons* assembly code and copy one of the existing data structures like OBJECT_DATA_HEALTH in order to keep the class attributes (the *ObjectDataTypeID* one is necessary to identify the structure as an *Object Component*). Rename your structure (following the naming pattern of OBJECT_DATA_[WHATEVER ELSE] is recommended but not strictly needed) and give it whatever data you see fit, as long as the structure remains **Unmanaged** (IE no directly or indirectly containing a reference type). Once done it will be automatically detected and used by the *ObjectMemoryManager*, will have a *Type ID* and will thus be assignable to *Objects*. 

Finally, the *Objects* themselves are also contained within a single block of memory within the *ObjectMemoryManager* as well. Since all are allocated at the start of the Match, and because the max amount of Objects being active at the same time is a known constant, this means we can easily iterate over the *Objects* and their *Owned Data* given that we have access to the *Objects* array and whichever *Container* we want to read from or write into. This is what *Workers* do.

#### Worker system

In order to run the simulation, some of the work is done by *Managers* directly, in a way that was never meant to be multithreaded or otherwise broadly optimized. Manager code can technically achieve anything, but it is "trusted" to be relatively lightweight. However, no matter how performant our algorithms are, the sheer amount of work can get phenomenal with a high number of complex Objects. Multithreading was going to become a necessity eventually (although to this day it still was not necessary to add any of it...), and thus the framework needed a design that would make it easy to parallelize as much of the work as possible whenever we got around to doing it.

Introducing: *Workers*. Worker objects represent "units" of work within the *Worker Pipeline* which is the bulk of the work needed to run a single Tick of the simulation. *Workers* are "Singleton" objects at the scale of a *Match* object: although possible, there would be no point creating multiple *Worker* objects of the same type within the same Match. *Workers* are held by the owner Match inside a List, which determines the order in which they run. There are 5 stages in the **Worker Pipeline** :

- Frame Begin: the *Worker* can do work when the Tick / Frame begins. However, it runs before any other stages on any other *Worker* types, meaning work that depends on other *Workers* running first can't be done here. Once all *Workers* have ran their *Frame Begin*, the *Pre-Work*->*Work*->*Post Work* stages start.

- Pre-Work: the *Worker* can do work before it starts iterating over every *Object*, IE this stage only runs once like the previous one. However, at this point every worker has ran their *Frame Begin* stage, and all *Workers* higher in the list have ran their *Pre-Work*, *Work* and *Post Work* stages. This means dependencies on other *Workers* have been "resolved" for this Tick at this point.

- Work: the bulk of the actual work for most *Workers*. Runs once per *Active / Living Object*. On each iteration the *Worker* is given an *Object ID* it can use to query data through a protected access function all *Workers* have called *FetchObjectData(Type, ID)*.  

- Post Work: same as *Pre-Work*, except runs after *Work*. Following this stage, the next *Worker* in the list's *Pre-Work* begins. If this was the last *Worker* in the list, then every *Worker* proceeds to run the next stage.

- Frame End: Every *Worker* has ran the rest of the pipeline at this point. This can be used for "final cleanup" work to prime the Worker for the next Tick, but before it actually begins.

Note that **every** stage of this pipeline run **after** *Managers* get updated.

The point of this whole system is to be able to add new features in a way that allows extreme granularity in the actual data processing: *Workers* can get quite complex but it is usually not difficult to split them into smaller *Workers*. To help enforce this idea, *Workers* are, in spirit, forbidden from maintaining a direct reference to the *Match* object or to any of its *Managers*. **All data references the *Worker* is going to be reading from or writing into must be cached on construction, which happens at the start of the *Match*. Usually they need to cache a reference to the underlying array of data owned by *Containers* in the *ObjectMemoryManager*.**

Although currently a single thread executed the whole pipeline for every *Worker* following the List as declared by hand in code, you can probably see how easy it would be to automatically figure out **dependencies** and organize the *Worker* objects into multiple "layers" of parallel execution. At least, that's how it seems to me in theory. It also allows for neat organization of gameplay features into bite sized code classes that can easily be disabled by simply not creating the Worker during *Match* construction.

Currently *Workers* already do a range of tasks: processing object movement, combat, unit AI, spawning from special buildings, death, target search, unit collision... As an example, it only took me half an hour to implement the latter! Granted, it is a "dumb" implementation, but it could be made to benefit from the same optimization *target search* does, whenever I get around to it.

*Creating* a new worker type from scratch is actually pretty simple: define a new class within the *Commons* assembly that inherits from *Game_Worker_Base*. Satisfy the few requirements of that base class, and then simply override any of the 5 pipeline functions you want your worker to use. If your *Worker* needs to read from or write into any external data (which it likely does...) then fill whatever references to it in through construction parameters which you can then satisfy within the *Match* construction code. You might take notice of the *IObjectDataContainerHolder* interface, which defines the ability of returning a *DataContainer* of any type. This essentially allows passing in the *ObjectMemoryManager* itself into the *Workers*' constructors without breaking the "no direct reference to the *Match* or its *Managers*" rule which simplifies the constructor prototypes greatly.

Usually, the references you will need cached inside a *Worker* are simple array references into Data owned by *Containers*, but it can really be anything else. One *Worker* even has a reference to a *Function* so that the code for it could be written inside a *Manager* without breaking the rule.

#### Synchronization

At this point you might be wondering how all this gets synchronized during Online play. The main thing to understand about it is that going for what I like to call a "Valve solution" for online synchronization was not a possibility here. As a reminder, here's the main idea behind a "Valve Solution”:

-> Player joins game, receives data from the server about the data they need to load (Map, objects, sounds...)
-> Once the Client has loaded everything it needed, it has been "primed" and is ready to join the State Updated group of players connected to the server. That group of players received a full update of the Game State many times per second (usually featuring the position, rotation, health... of every active object on the server).
-> The Server simulates everything and sends the result to every Client. Clients therefore only must show the data as best they can, and maybe run some inter/extrapolation to smooth the experience out.

This approach has pros and cons:

Pros:
- More instinctive to implement with an Object-Oriented approach: you have a bunch of objects running some code, and every frame you take their current state and send it to everyone else (if you're the server). Easy!
- Desynchronization is less of an issue the more "complete" each game state update is. You can potentially update *everything* on every tick and be entirely shielded from desynch problems so long as connection persists.
- Since the Client's state only / mostly relies on the Server's latest update, it means joining an ongoing game is very easy to accomplish: just load the (usually static) data the match "started with" (map, game mode... whatever the server doesn't keep "reminding" the clients about) and then hook the newly connected player to the State Update system.

Cons:
- Very dependent on a strong connection, especially for fast paced games. Since games of that nature are not usually very deterministic (the amount of different things that CAN happen with 60 players each controlling a character with a gun is... pretty high) the client is totally reliant on the server updating it about what's happening EXTREMELY frequently, and any lag will likely mean a worse playing experience.
- **Limited on how complex or big the game can get (and that is the main issue here). Because the server needs to keep sending updates about most if not every object in the game, the amount of data can't get too high otherwise bandwidth gets overloaded.**


Because of this last drawback, the Valve approach is not feasible. Imagine a match involving just 4 players each making an army of 200 units. That is a LOT of positions, healths, mana bars, attack states... to update the clients with. The strategy I ended up using for this project is called *Lockstep*.

The idea of *LockStep* is relatively simple: assuming the game simulation is FULLY deterministic outside Player input, you end up with the following property.

Take G(T) as the function determining the Game's state at time T. Take G[X](T) as the game's state at time T for client X, or the server for G[S](T).
For clients 1, 2, and server S:

Assuming no player input happens between times T1 and T2, and assuming G\[1\]\(T1\) == G\[2\]\(T1\) == G\[S\]\(T1\), then G\[1\]\(T2\) == G\[2\]\(T2\) == G\[S\]\(T2\).

If any player input happens at time TI (with T1 < TI < T2), assuming it gets considered at time TI exactly on every client and server, then the relationship holds true.

This has the implication that, so long as the starting data is the same, then the entire game can be synchronized just by synchronizing player input actions (that by nature cannot be predicted). This means very little data is needed to synchronize even big simulations with a lot of moving parts: let every client & the server do the work instead of overloading the network bandwidth is the idea.

The pros & cons are basically the contrary of the Valve approach. On top of that there are three challenges to take on due to the complex nature of this approach:

- The game simulation must be perfectly deterministic outside player input. This means random number generation is much more difficult to accomplish (as the same number would have to be generated on all ends and be generated again if we ever need to go back in time for a replay or "catch up" system). In some cases, problems as small as float imprecisions can create desynch.
- Any non-deterministic events (player input & cheat codes) must be recorded and synched in a way that makes it be taken into account at the exact same theoretical time on every machine running the simulation.
- If any desynchronization does occur... safe to say trying to restore that game to a working state will be a headache.

In our case making the game simulation fully deterministic on all ends wasn't a great challenge : we're building an RTS so as long as we avoid using randomness in, say, unit AI or combat, we're fine. On both clients and servers, the exact same Match code is running, with the exact same memory management and the exact same Workers. That does not guarantee synchronicity over time of course, since we are lacking an enforced tick rate - meaning slower machines do tend to stray away from what higher end machines simulate over time. But it is a start.

The main challenge was finding a way of easily synchronizing changes in data related to player input. What happens when a player orders some of their units to attack? This is where *DataChangeEvents* come in.

A *DataChangeEvent* is the expression of a change in data that "wants" to happen. It features the type of data it wants to change (through the same *Type ID* as in the *Object Memory* part), the affected *Object IDs* and the new value for that data type. Later it could also contain a timestamp of when the event was generated on the player's end as opposed to received by the server.

The idea is that the purely Client-Side code generates these events through any means - UI, key binds, selection system... and sends them to the *Server Match*... and here is where it might get confusing:

- When playing multiplayer, as we'd expect, although we are running our own *Match* object locally, the event gets sent through channel 1 to the *Server*, who receives it, potentially runs sanity checks (to make sure we're not trying to control units we don't own for example), and at the same time "broadcasts" the event back to every client (*including* the one who generated it) so they may also apply it on their ends.

- When playing singleplayer... the event gets sent to the Client itself. Yes, it sends something to itself. The idea is that the Client "gameplay" code such as the *Unit Selection & Control system* shouldn't have to worry about whether the events it generates should be sent to a server or be processed by the *Local Match* directly. When it is the latter, then we "trick" our Client into believing it just received an event to process from the "server" by sending it through channel 0, the "Self" *Communication Channel*.

This explains why there is a "Self" *Channel*. The third *Channel*, "Local Match Channel", exists because sometimes the *Match* object itself generates event messages. To avoid feedback loops of sending -> receiving -> sending as a reaction -> receiving again *ad infinitum* the *Local Match* always sends events generated by itself through a specific channel, whose *received* content it ignores entirely.

And that's for now all there is to say about synchronization in this framework. It is very crude as yet, but it already works well enough for primitive testing and has the advantage of being *extremely* flexible: the *ObjectDataChangeEvent* system can work with **any** type of *Object Data*.

#### Feature examples

Every feature of a *Match*'s simulation is built on *Managers*, *Workers* and presumably some *Object data*. Let us go through some of the currently implemented ones.

##### Unit movement

A *Unit* is understood to be an *Object* that can move, interact with other *Objects* in some way, be controlled by a player or AI, and be destroyed. In this case we are interested in *Movement*, how is it done?

There are four elements at play: the **AI** data component, the **Movement** data component, the **Movement Updater** *Worker* and the **Order Movement Processor** *Worker*.

- AI data contains an *Object*'s current Order and data associated with that Order. It thus also *enables* that Object to be affected by AI-related *Workers*.
- Movement data contains movement stats, and the *Object*'s current destination (not necessarily where the AI *wants* to go, but where it's heading to *right now*, possibly as part of a broader pathfinding algorithm)
- The Movement Updater *Worker* simply iterates over every *Object* that owns a Movement data component and **a Transform data component**. It checks the related destination data, stats, and moves the *Object*'s Transform data component's position attribute accordingly.
- The Order Movement Processor iterates over *Objects* that own an AI data component and a Movement data component. As its name implies it updates the Movement component depending on where the AI "wants" to go (towards an ordered position, towards a targeted enemy...)

While the data components get detected automatically, the *Workers* must be added to the *Match* construction code (which is part of a broader flaw of the framework I'll touch on later). Note that the *Workers* in this case do not have to be separate - you could totally make a single one that does both the AI -> Movement data work, and the Movement data -> Position / Transform change work. However, the more granular your *Workers* are, the easier it is to reuse them for other features, and the easier it will be to **parallelize** simply because each *Worker* will have fewer dependencies.

#### Target search & Collisions (IE querying Objects in area)

In an RTS, a LOT of what happens is based on how far apart things are from each other : aggro range, cast range, attack range, vision range, collision radius...

From the start I knew I would eventually have to come up with the best system I could design in my junior mind to reduce the massive performance cost of... things interacting with other things depending on distance. If you've been tempted to code AI battles in your Unity scenes before, you might not know that optimizing search by distance is a **big deal**.

The solution(s) is a type of solution called "Spatial partitioning", as in making the relevant data quickly accessible through a world position. This usually involves "physically" (in memory) placing / pointing to your data in a way that mimics the actual placement of your data in the world.

A very simple, yet very effective implementation of this is what I call a "Cell system”: your map gets divided in "Cells" on the X / Z 2D plane, sometimes all of them square (thus forming a grid) or sometimes shaped differently (to follow the map's layout better for example, especially if you want to use the same Cell System for pathfinding). The principle is simple: through some algorithm, you keep track of where things are, and place them in memory that is quick to access from a given cell depending on their position. A "naive" implementation (when performance is not critical) is to assign a container of *Object* / *Entity* / *Whatever really* references to the cell, preferably of a type that has a dynamically resizable *length*. With that done, you've already massively optimized performances of target search algorithm (among many others) because now you can very easily cull distance comparisons against *Objects* that could not possibly be in range to be affected (for example, when looking for the closest enemy, you can stop searching Cells *further away* than a cell where you have found a *potential target*, since they couldn't possibly be closer).

My solution is similar: the same concept of *Cells* is present, except the way they "point" to what's inside of them works differently. See, for the same necessity of *data locality* that I had to deal with when building my *Object Memory* system, I could not go with assigning a dynamically resizable container for each cell, as that would spread the actual data out in memory in an unacceptable way (at least to my "*I need to optimize this even if it was never going to be a problem to begin with*" mind).

The first working implementation simply gave each cell a fixed size array of *uint* data, which was meant to contain the IDs of *Objects* located within this cell. This had two problems:
- The maximum amount of *Objects* a *Cell* could hold was **fixed**. This meant that if ever too many *Objects* entered the same *Cell*, the whole simulation would crash. Guess what kept happening when I was playing around triggering big battles on the map?
- It was extremely memory inefficient, to an unacceptable degree, even on *modern hardware*. Imagine a map with 50 x 50 cells... that's 2500. Times 10. 25 000 "spots" to hold *Object IDs* in (taking a total of 100 kB...) when *the maximum number of Objects that could possibly even exist was capped at 1000*.

As such I ended up scrapping the frankly lazy idea of constant memory for each *Cell*, and instead came up with this:

*We can only have a fixed maximum amount of Objects at once over the entire map.* What if a memory pool big enough to hold as many IDs was allocated and then *shared* by the *Cells* ? It would theoretically work since if an *Object* enters a *Cell*, then either it was spawned, meaning we have memory left, or it *left another Cell*, freeing up the memory we need. Moreover, it being a single sequential block of memory means that it will works *wonders* with the CPU cache. Especially when iterating over that memory, looking for a target or affecting things in range of something else.

**Now, of course, actually implementing this would prove very difficult.**

Here is how it works: picture each *Cell* no longer as its own construct with its own local memory for holding *Object IDs* in, but rather *property owners*. *Cells* can *Own* a part of the *Common memory* or *ID slots memory*. They are essentially fancy pointers that do not actually use pointers (at least not as class fields). They contain three numbers:

- The ID of the first slot they *potentially* own (important detail: *Cells* can end up owning **no memory** / **no slots** at all)
- The amount of *slots* they own, and are taken by an actual *Object* (as opposed to *free slots* which are always of value *uint.MaxValue*)
- The amount of *slots* they own in total. The amount of *Free slots* can thus be calculated easily.

Finally, *Cells* follow one rule in how they are laid out in this common memory. Their *right* and *left* neighbors must correspond to the *next* and *previous* neighboring *Cell* in the grid, IE X+1 and X-1 respectively (except for edge *Cells* that have a neighbor inside another row). Thus a *Cell* will always be "*placed*" in the common memory *right after* its previous neighboring *Cell* and *right before* its next neighboring *Cell*. As an example, picture a 2*2 *Cell* grid with a total of 12 slots to share. If the *Objects* are perfectly spread among the *Cells*, you would get this ('|' represents the separation between two ID slots in memory):

(Cell 0 start) 0 | 1 | 2 | (Cell 1 start) 3 | 4 | 5 | (Cell 2 start) 6 | 7 | 8 | (Cell 3 start) 9 | 10 | 11 

Makes sense, right? Here the actual IDs do not matter, you could place them in any order. All that is important to notice is that the memory is **filled** because none of the IDs in memory are equal to the maximum value a *uint* can hold.

Now, what happens if every object moves to Cell 1? This is how the memory what the memory would look like:

(Cell 0 start) (Cell 1 start) 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | (Cell 2 start) (Cell 3 start)

Remember that each *Cell* keeps track of how many slots it *owns*, so there is no risk of mistakenly thinking *Cell 0* owns anything. You might also notice that it is possible for *Cells* to be placed in the same location, including after the last actual slot.

One last example: half of the *Objects* move to Cell 0 and half of the others move to Cell 3. If you have understood everything so far you should be able to picture the memory distribution. If not, here it is:

(Cell 0 start) 0 | 1 | 2 | 3 | 4 | 5 | (Cell 1 start) (Cell 2 start) (Cell 3 start) 6 | 7 | 8 | 9 | 10 | 11

I hope it is starting to become clear.

I will not go into detail as to how the automatic redistribution of memory is implemented. If you want to (try to) find out, you can visit the code. It's all in the CellSystem file IIRC. All there is to understand, in principle, is that the sum of all "movements" in a *Tick* are resolved by a dedicated *Worker* that runs before any other. From these *movements*, the new number of slots each *Cell* **needs** is easy to determine, and if ever a *Cell* ends up not owning enough memory, it is able to "*ask*" for more from its left and right neighbors, which might trigger these neighbors to do the same (hence propagating the request for memory, sometimes actually finding some *hundreds* of cell to the right or left. Thankfully that part is not performance critical, and by nature is difficult to overload for long).

Finally, for checking *whether* Objects have moved from one tile to another, I have not yet felt the need to do anything smarter than using a *Worker* (the same as the one generating the *movements*) to iterate over every single object in possession of a *Cell position data* and a *Transform data* component and checking their current position compared to their current *Cell position*. It could be cleverer in not checking *Objects* that cannot move for example, but unless there are tons of *Objects* that keep moving around I really don't see this *Worker* ever being overloaded, and it'd be **very easy** to parallelize as it runs *first* and has very few dependencies.

**With that implemented, it was very easy to implement target search: for every *Object* with an AI, an owner (IE a "side"), and a weapon (IE a reason to target things...), AND a Cell Position component, check their Cell's coordinates in the grid, and start a search algorithm that gradually looks inside further and further Cells until potential targets have been found, and only THEN run any actual distance comparisons.**

Having access to this *Cell System* that automatically takes into account all *Objects* with the right components is a massive advantage in implementing any feature that cares about where things are... which is a LOT of features. With this system I managed to also keep it all "Cache-friendly" AND memory efficient, as now the size of that memory pool depends on how many *Objects* at once we want to be able to support, rather than the number of *Cells* there are.

#### Spawning ~~units~~ Objects from some ~~buildings~~ * other Objects*

To take a breather from complexity, an example of a very simple feature that I got working in very little time: automatically spawning *Objects* from other *Objects* at a certain frequency. As I have not yet delved into *Game Data* and *Object types* / *Object Archetypes*, I cannot go into implementation specifics. All you need to know is that an *Object type* is a number-identifiable (as in, *it has a global ID*) set of *Data components* an object should spawn with, along with default values for it, as well as common stats and characteristics you would expect like a *Name*, and some more data that helps the *Client* identify which graphical assets to use for that *Object*.

To implement this feature, all it took was two elements:

- A data component I would call the *Periodic Spawner Data* component, containing the *spawn period*, the *current spawn cooldown* and the ID of the *Object type* we want to spawn
- A worker to update these components and trigger the actual spawning

As always, the data component was automatically detected. The worker only involved a small amount of code and references to the *Spawning Queue* (because *Workers* are not allowed a direct reference to the *Object Memory Manager* which handles the actual spawning, they are given access to a *Queue* which takes in "Requests" to spawn things if they need to, to which they can keep a reference exactly like *Object data*), the *Object data* for *Positions* (so that we know where to place spawned *Objects*) and the component we created.

Our new *Worker* then simply implements the *Work* stage of the pipeline in order to iterate over every *Object* with the relevant components. Their *current spawn cooldown* gets updated, and if it's found to be below 0, then it gets reset back to the value of *spawn period* and a single spawning request for the relevant *Object type* is logged to the *Spawn request queue*. Not counting all the more abstract things I had to implement to get it to work, it must have taken me... 10 minutes of work ?


There is one more major element we have not gone to yet: *Game Data* elements (currently made up of *Object Types* and *Archetypes*). They are technically part of the *Commons* but a in-depth explanation will be provided in the *Editor Code* section of the next part.

## Understanding the code base (part 3: Client systems)

The *Client* is the second biggest project in terms of code. It also contains the Editor code, used for creating and editing *Game Data*. Let us go through every major system.

### Client States (Game flow)

Very quickly in development, a need arose to split the Client management code (at the time mostly concentrated within *GameClient*) within multiple classes housing code for the set of data and behaviors needed when the *Client* is in a certain... *State*.

A *Client State* does not pay taxes or swear homage but it does simplify the construction of the *Client* app in how and when different sub systems get initialized and (re)activated. They can handle any kind of behavior - *MESSAGE* based behaviors, UI interactions (through *UI Modules*), *Manager* interactions (States possess their own *Managers* on top of the *GameClient* singleton object itself also possessing his own. The difference being *GameClient* Managers are permanent, and *Client State* Managers only persist as long as the State itself does. Both can be accessed through the *GameClient*'s *GetManager* function), and sometimes debugging.

### UI Modules

Making a Game that turns out to be more of a Framework means that the UI is left in a very uncertain state. You might make a new element, and notice you need to reuse it in multiple places, have variants of it, or change it in depth without affecting the rest of the interface. With this in mind and the personal rule I have of *never* mixing *Representation* code with actual *Work code* (Frontend / Backend, or *View/Controller* design as you might call it), I came up with the *UI Module* system.

A *UI Module* represents a single "unit" of interface. It might be a single element with special functionality you might reuse many times over on the same screen (Unit HP bars for example), or a more complex element that could be used only in a single place, or a mix of both.

Each *Module* exists in the project assets as a Prefab object. The parent object (usually a *Canvas*) must contain a Unity *Component/Monobehaviour* script of a type that derives from *UIModule*. It must also be placed within the *Resources/UI/Modules/* folder.

Beyond that, the exact code a *Module* contains is extremely flexible, but the point is usually to build a class with the ability to call events things can register to. That way, they can be used productively by whatever backend code (usually a *Client State*) created them. *UIModule* such as it is a completely empty *abstract* class.

To make use of *UIModules*, a special *GameClient*-wide *Manager* called *UIManager* takes care of automatically loading every single valid *Module* within the correct folder into memory and making them easily accessible through type, name, or both depending on what type / variant of a type we want to use. Then, UI users can simply call one of the overloads of *GetUIModule* which searches for and instantiates the appropriate *Module* if it exists. It then returns a reference to it. 
For now, whatever calls this is entrusted with *caching* the reference to the *Module* they have just created and *Destroying* it. The initial idea though was for that to somehow be done automatically, depending on a "lifetime" parameter passed when getting the *Module* (it would work with the fact most *Modules* get destroyed when the current *Client State* ends or when a specific *Pawn* dies. More on *Pawns* later). That way, given that most *Modules* can only be interacted with by registering to events, users of these modules would not even need to keep a cached reference to them. Get the module -> Register for events -> Forget about it would be the go-to for most cases.

Note that *all* instantiated *UIModules* get parented to the global *\[UI ROOT\]* object in order to not clog up the hierarchy (especially in the case of *Pawn UI Modules*, which are not parented to the same object. More on those in the *Pawn System* section).

### Local Match

As we make use of the *Lockstep* system, a full *simulation* of a *Match* is ran on the *Client* no matter if we are playing online or offline. The *LocalMatchManager* is a *Manager* whose role is to wrap the *Client*'s unique *Match* object, initialize it, update it, transmit messages...

That *Manager* exists only during the *PlayingState*. If playing Singleplayer, the state creates the *Match Start Data* and transmits it to the manager to initialize the *Match* with and start playing. If playing Multiplayer, the *Match* and its Manager are primed to receive that data from a *Message* instead, and until that point are put in a waiting state. Once overhauled the *LocalMatchManager* will probably be created and primed for either mode of play by a *LoadingState* with the *PlayingState* only starting once the *Match* has its data and is starting its update cycle.

### Pawn System

Another *Manager* belonging to the *PlayingState* is the *PawnManager*.

Its role is to manage the entirety of the *Pawn* system. A bit of context: on the client-side, we have a *Match* system whose code and functionality is entirely located with the *Commons*, meaning it cannot contain any code meant for clients to understand what's happening currently, but only the underlying data management.

A specific system on the *Client* was needed to be able to "Watch" the local *Match* object and *represent* the current game state to the best of its abilities, mostly the current state of *Objects*. *Pawns* do just that.

A *Pawn* is strictly speaking the "Unity side", GameObject-based display of *Objects*. Depending on its type, it *watches* specific data related to an *Object* it is responsible for *tracking*. All *Pawns* watch their *Object*'s position and update their linked *Pawn UI Modules*. However, all instantiated *Pawns* have to be from a derived type, with more specific behavior.

All *Pawn Types* exist as a prefab within the project assets which possesses the associated code on its root object, and the default *Actor* for that *Pawn Type*, something I will not get into in great detail (*Actors* are basically wrappers for graphical content that can be assigned to a *Pawn* and receive string-based events from them). The *ObjectPawnManager* automatically loads every *Pawn Type* and *Actor* the first time an *ObjectPawnManager* is instantiated. From there, it will watch the local *Match*'s memory and react to new *Objects* being spawned, determine which *Pawn Type* and *Actor* to use (something it does through the spawned *Object*'s *Object type*) and finally give the newly instantiated *Pawn* a chance to start watching data by passing it the local *Match*'s memory manager through the *LinkToGameData* function.

#### Pawn UI Modules

In order to be able to display information about a *Pawn*'s tracked *Object* using UI elements, the *UI Module* system was adapted to be usable on the *Pawn* level. Very simply, *UI Modules* deriving from the type *PawnUIModuleComponent* can be used by *Pawns*. These special *modules* will update their position on screen automatically, staying at a certain (parameterizable) offset from their target *Pawn*. They are parented to a specific object, itself located below the global *UI Root*. They are, finally, automatically discarded when the associated *Pawn* gets destroyed.

#### Current Pawn types

Currently, only a single *Pawn Type* exists that significantly expands on the behavior and tracked data of the base class: *Unit Pawns*.

A *Unit Pawn* makes use of a single *Pawn UI Module* a *health bar*. It simply makes sure to update its current value (from 0 to 1) depending on the associated *Object*'s health and maximum health.

It tracks a lot more data than the base *Pawn* type does: the *Object*'s current AI data, health data, movement data and weapon data so that it might play the proper animations when a unit moves, attacks, takes damage, dies... 

Another *Pawn type* exists that does not add any functionality from the base class, used for objects which do not feature any animations, and do not need to display any information beyond simply being at the right position and having the right model.

### Editor code & Game Data

In multiple places in this document, you've seen mention of the *Game Data* layer which mostly exists within the *Commons*, as it is used mostly by the *Match* code. Nevertheless, I decided to not speak about it in detail until reaching this part, to put it in context with the Editor code used to create and manipulate this data, as well as export it to a format both the *Server* and *Client* apps can read.

*Game Data*, rather confusingly I admit, only designates data that *links* together the different resources (data components, pawn types, actors...) the game is shipped with. This is what allows us to bring order to our codebase and resources to make it into an actual RTS game with *Units*, *Buildings*, *Destructibles*, *Abilities*...

It is generated with the help of *Scriptable Object* types in the *Client*'s project assets (under "Assets/Bulk/Game Data Building/") with specific editor code, and other assets like the *Pawn Type* and *Actor* prefabs.

Currently, *Game Data* contains the following:

- Object *Archetypes* that define a set of *Data Components* and *default values* for them
- Object *Types* that define an *Archetype*, possible overrides to the *Archetype*'s component types and / or default values, a name, an *Actor* and a *Pawn Type*. The last two being defined through an ID that is meaningless on the Server side but allows *Clients* to construct the correct *Pawn* for a given *Object Type ID*.

On the editor side, things are linked through references to one another and simple serialized data. Once the data gets exported from the *Game Data Window* (which you can find by opening the *Client* Unity project and going to the *Tools/Game Data/* menu), everything gets resolved to an ID based system and encoded in a custom-made binary format called "gameDat". The exact way the format works can be found in the *Commons*, under the "Resources" folder.

Given the files were exported and distributed appropriately, the *Client* and *Server* applications will detect and load them on startup, and you will then be able to spawn *Objects* using an *Object Type ID* rather than a full definition of the Object's *Data Components*. 

### Example of a Client Gameplay system: Unit selection & ordering 

The main gameplay element of RTS games is usually selecting your units and right clicking the place you want them to move to.

It is implemented through the *RTSModeUnitControlManager* and the *SelectedPawnsContainer* and *UnitSelector* helper classes. Simply put, the manager makes use of the helper classes to determine at any one time which *Pawns* are currently selected by the Player. With that information, the manager is then able to generate a *DataChangeEvent* (remember those ?) whenever the player right clicks, "asking" to change every selected *Object*'s AI data to the "Move To" order type, and the clicked position as data. That event is then sent as a Message to whatever is currently running a *Authoritative Match* (a notion I decided not to expand on. Multiplayer = the *Server* is authoritative and every event requires its say so before being applied on any machine, Singleplayer = the *Client's Local match* is authoritative and receives the event through the *Self* channel) so that the event might be taken into account, and in the case of multiplayer, broadcast.

## Understanding the code base (part 4 : Server systems & the initial net architecture)

### The initial vision

The *Neo-RTS Framework*'s vision is one of high scales, and that includes the number of potential players interacting as a community for your game. Thus the **net architecture** has to be **highly extendable**. This means that a overpopulation / underpopulation problem should be fixable by simply adding / removing server machines, and that the set of tasks servers have to collectively accomplish must be spread among different *types* of servers in a clever way.

The initial vision for the framework worked as follow:

-> Client app: one per player. Allows a player to log onto a *Master Server* and, temporarily, to a *Match Server* in order to play online. With a potential ability for *peer-to-peer* play as well.
-> *Master Server*: "Regional" servers which are more like *communication hubs* than actual *servers*. They each possess a (possibly mirrored) database containing player profile information, and their addresses are *ingrained* into the client apps. It is where players meet and start playing matches with one another through *matchmaking* or *custom games*, which triggers them to connect to a designated *Match Server* the *Master Server* knows about through an actively updated list of connected *Match Servers*.
-> *Match Server*: Servers doing the bulk of the work required to run *Matches*. Players connected to that server are only connected as long as required for the *Match*. *Match Servers* need to be acknowledged by a *Master Server* by connecting to them.

This is thus a small architecture, but it seemed to me like the simplest possible one where whoever manages the game can easily scale the resources allocated for online play according to population. There might be a need for "helper servers" for the *Master Server*'s work if population gets really high (though I believe you could pretty easily handle thousands of players with a single one if it is well-multithreaded). 

In its current state, the framework does not yet feature a *Master Server* / *Match Server* dichotomy, but a single *Server* app that handles both. Note that because of the *Manager* object design, it would be very easy to split it right now, hence why I did not rush that separation along. An initial separation could be done by running both server types on the same app and have them communicate through a *LocalAppCommunicationChannel* similarly to how the *Client* and *Server* would in the early stages of development.

Let's go over the set of services the *Server* currently provides or could provide fully with minimal development time :

### Connections & ConnectedPlayersManager

If you have not read about *Communication Channels* objects yet, I encourage you to do it now. Long story short, a *Communication Channel* object is a wrapper for a certain *Communication protocol* with a specific *interlocutor*. On the Server, a list of these objects is maintained and dynamically expanded / reassigned every time *something* connects to the server (which it picks up on through a specific *Manager* that maintains a *listening socket* on port 27015 by default).

This means that, initially, the "*link*" to a player connected to the server from the point of view of any of its services (think matchmaking queue, chat channels...) would exist solely through the channel object created upon their connection. This was problematic, because internet connections to "random" users are by nature unstable and you can sometimes get scenarios where you don't necessarily want a player to be fully "unloaded" from the server if they reconnect 10 seconds after disconnecting. You also might simply want to create and manage *session data* until the player has disconnected for at least a certain amount of time.

To solve this problem, there was a need for a layer of "insulation" from the instability of *Communication Channels*. It is provided by the *ConnectedPlayersManager* whose role is to react to the server receiving a *Message* of *Type/Header* "Authentification", which as of now only contains a player's name. The manager works with these messages to manage a set of *ConnectedPlayer* objects by creating & discarding them (while giving notice to other services) and update the *Channel* they are linked to.

Other server services can thus completely ignore the dynamics of *Communication Channels* and instead use the *ConnectedPlayers* objects through events fired by the *ConnectedPlayersManager*. Through the *ConnectedPlayer* objects, the services can send & receive messages, and react to "proper" connections and disconnections AND "channel" disconnection events separately. 

### Matchmaking & MatchesManager

The *MatchmakingManager* and *MatchesManager* together manage the online gameplay experience of connected players. They both make use of the *ConnectedPlayer* objects, the former to register to receiving a request to start or stop *matchmaking* (which simply puts the *ConnectedPlayer* inside a list of players who want to play), and send a notice when a match was found. The latter uses it to properly create *ServerMatch* structures (which are wrappers for a *Match* object with associated *ConnectedPlayers*) and set up the message sending (which, because it was developped before the *ConnectedPlayer* system, does not make use of it but rather uses channels directly for now) in order to broadcast *Data Change Events* and more.

The *MatchesManager* generates the *Match Start Data* of every *Match* and sends it to participating players. Once created all the *ServerMatch* structures (within which the *Match* objects exist) stay in a list and get updated by the server. For now, they only end when one of the associated *ConnectedPlayers* disconnects.

### Chat

The only other service the server currently provides is a simple chat box system. It also works with the *ConnectedPlayer* system.

Very simply, it keeps track of *ConnectedPlayer* sending messages requesting to join / leave chat, and send a chat message. All *ConnectedPlayers* that are currently considered to be in chat get every chat message broadcast to them so they may display it in the appropriate chat box on the client side.

It probably won't stay this way, as it was made quickly to provide some communication system for players connected to the server.

**And that's it for the code base! It took a much longer time than expected providing a relatively exhaustive view of what we've accomplished in around 150 hours. To date this is very likely the most complex solution I've ever built. This means that whenever I stop being proud of it, I will have reached the next step in becoming a better developer. How exciting...**

## Will I continue this project? What would I do better if starting from scratch?

Before ending this document, I wanted to write about the future of this project, or rather why I won't keep working on this code base.

This was entirely developed on stream and uploaded to YouTube. This means that this project only continued so long as I felt like it made for good content - which I eventually decided it did not (I wouldn't recommend going for long winded projects as Twitch content as the more you advance the less welcoming it is to newcomers).

Beyond that, I think this project has already made me learn many new things and make me fiddle with nearly every aspects of game development a Unity Developer might have to fiddle with (with a few exceptions like advanced animating, shader development and net security). So stopping now, I still feel like this project was far from being a waste of time even when taken outside the context of streaming it.

Finally, even so early in development I already feel like I made a few mistakes that would justify starting from scratch if I ever wanted another go at making an online RTS framework:

-> The "Framework" nature of the code didn't become apparent right away: I originally set out to make a *game* and ended up making a *framework* first because of my rule to not use third party libraries. However, the two natures of the code I was writing conflicted, and now the "*framework*" I've produced isn't actually very usable as a framework, as you need to tangle with the actual *Commons* to change even very basic things, whereas a good framework in C# should be fully usable without changing its core code. For example, starting again, I would make sure to be able to add *Workers* and *Data Components* seamlessly from outside the *Commons* assembly. 

-> Because, in part, of my own ignorance of what C# can do (IE I didn't know about the BinaryWriter/Reader objects) and my awakening to C++ happening in parallel, there is a lot of *pointer code* (or *unsafe* code as it's called in C#), sometimes for reasons of performance, other times for encoding / decoding messages. In the meantime, I learned to write C++ native dlls and starting again I would probably write the bulk of the *Commons* as a C++ native library (especially performance critical code like the *Match Code*), with the C# part being a wrapper for usability by users of the framework.

-> Finally, simply put: there was a complete lack of a longer term plan. No *Trello* or *Hack'n plan* board, no commit discipline... This means that a lot of time was spent working on frankly non important features and wondering what exactly to do next. I would also have liked to present the project over time in a more accessible way than hours long VODs on YouTube.

Thanks for reading! I will continue streamed development here and there on my Twitch channel (https://www.twitch.tv/Hoshiqua), with "one-off" series where I try to "solve" a certain problem on much smaller projects that get worked on for only a handful of streams.
