# Project-NeoRTS

## Streamed Online RTS Project.

### Link to current Client build : https://drive.google.com/file/d/14_UJcRmgkqBtBGLjMqTX6O8486oE9Dlb/view?usp=sharing

Note that the game isn't in a playable state : all present interactive elements are built as a test, using the Framework beneath as a service of sorts. 
Development was entirely streamed on my Twitch channel : https://www.twitch.tv/Hoshiqua
... and entirely uploaded to my Youtube channel : https://www.youtube.com/channel/UCvdyDp5GVFhEls_3hy7M38A

Becauses of changes to my stream schedule and format, development is for now suspended. Video presentation of the current codebase coming soon.


## Understanding the code base (Pt 1 : "Application" projects)

The Neo RTS framework is my attempt at building my very own Online RTS game in the cleanest way I could achieve, without using any third party libraries other than Unity itself, and only for the Client (no Unity code is strictly speaking used outside the Client code base, although they might still have a reference to it that I forgot to remove).

The only language used accross the entirety of the code (to my regret, see the "What could be improved" section) is C#, including so-called "unsafe" code. All network communications go through my custom made network solution that relies only on the C# socket API, for now using only TCP.

The code is split into three different projects : the *Client*, the *Server* and the *Commons*.

### Client

The actual Unity project under the main NeoRTS folder. The code can be found under NeoRTS/Assets/Sources.

The *Client* is what the actual player needs to play the game both online and in singleplayer. It includes the *Commons* but **NOT** the Server code. It features mostly code to graphically "represent" a game's state (through the *Pawn* system), UI code (through the *UI Module* system), a global State Machine to drive the user experience (mostly used for major menu navigation), connectivity to a Server, the ability of starting a locally ran, singleplayer game independent of any connection to a server, and finally on the Editor side some tool code to create & edit the *Game Data*. 

The *Client* is the only one of the three Project that directly relies on and runs as a Unity application.

### Server

Secondary console application project, can be found within NeoRTS/NeoRTSServer.

The *Server* is, in and of itself, just a console application able to listen for connection requests on a given port and provide services to Clients that authenticate themselves in order to access various services. It includes the *Commons* but **NOT** the Client code.

Once a connection is established, it expects to receive an Authentification message that, as of now, simply contains the Player's name and serves as a way to confirm that this connection *Channel* does indeed link to a *Player*. A special *Manager* creates a *ConnectedPlayer* object in reaction to that, which is essentially an interface object that allows the rest of the server's services to interact with that player in a simpler, more stable way (for example, although it is not done yet, it would be easy to insulate the server's systems from a sudden disconnect / reconnect event where it wouldn't be worth completely "unloading" the player. Think of "drop time" a lot of online games have).

The *ConnectedPlayer* keeps track of network *Messages* sent through its current *Connection Channel* and is then able to send / receive requests for :
- Matchmaking
- Joining and leaving chat, sending a message
- Anything else we might want to make, as the way it is implemented makes it really easy to iterate with new features.

## Understanding the code base (Pt 2 : *Common* code)

This project deserves its own part because it contains the majority of the framework's code and complexity. It is directly, and extensively used by the *Client* and *Server* code. It can be found in NeoRTS/NeoRTSCommons.

In spirit, the *Common code* or *Commons* is / are used as a service by the *Server* and *Client*. It has a variety of features ranging from the full *Simulation code* to run an actual gameplay session of our game, to utility code like common string encoding, mostly for sending chat messages over the network. That, amongst much more. When compiling the Commons project, a C# Framework DLL is built and automatically deployed to the relevant dependencies folders on the *Client* and *Server* codebases.

Since the *Commons* code base is relatively extensive compared to the other two, I will go through every major element that seem worth explaining, and describe their role aswell as the thinking behind them.

### Communications

Building an Online game, you are better off developing with the Clients / Server(s) duality in mind from the start. "Converting" code from Singleplayer to Multiplayer, whatever that means, ranges from headache-inducing to straight up impossible. This means the more you wait before putting your (and your teammate(s)'(s)) mind(s) in the paradigm of building a multiplayer game that will at some point have to rely on data synchronization and security, the more work you create for yourself down the road. And that's more of an exponential relationship than a linear one...

As such, my very first priority when starting the project out was to make it exactly so : build the code base around the reality of (potentially) having to communicate with *something* that isn't the app itself (*...at least not always, clarified later*) to run the game. Yet, I didn't want to tangle with *actual* Networking code right away.

Introducing : the **Communication Channel** object / layer of abstraction.

A *Communication Channel* object **class** defines the ability of creating a channel making use of a specific communication protocol, usually provided with the information it needs to find its interlocutor (the rule one channel object = one interlocutor isn't strictly enforced but it is what's expected). An **instance** is thus a "linked up" object that is ready to send and receive message with a specific interlocutor, using a specific protocol. All communications use the *MESSAGE* data structure, which is basically a fancy wrapper around an array of bytes, the first 4 of which being a **header** that defines the Message's type and purpose.

Although currently there is such a *Channel* type that uses a TCP connection and is thus network-capable, it did not come along until relatively late in development. For about a third of the total development time (~130 hours as of writing this), not a single line of networking code existed, and instead the client & server existed both within the same application (back before the code was divided into three different projects). To communicate, they simply used a *LocalAppCommunicationChannel* which had a very simple "protocol" : be linked with another such channel before usage, and then simply call the "paired" channel's Receive() function when sending a *Message* through.

It was crude, but it worked : the transition to networked communications was almost entirely seamless, and a surprisingly small percentage of the code base was really affected.

Nowadays, the *Client* even still uses that simple *Channel* type, and it's paired with... *itself*. And there's two of these channels. The reason for that will be explained in the Gameplay / Simulation section.

In order to create and decode *MESSAGE* data structures, a whole other layer of abstraction called *Message Packers* is used. *Message Packers* define an encoding & decoding scheme from a data structure of a certain "format" (simplest "format" being *unmanaged* structures that can all just be directly reduced to a single sequence in memory) to a byte array (returned as a *MESSAGE* to include meta data such as the **Header**) and back.

A single *Packer* type defines an entire encoding / decoding *scheme*, so it is not necessarily on a one-to-one relationship with a certain message *Header/Type* or with a certain data structure to be transfered. Some are, others like the *SimpleMessagePacker* are used extensively for a wide variety of purposes as it is able to work with any data structure deemed to be *unmanaged* (IE no direct & indirect underlying pointers or references are needed to implement it).

In usage, the Server and Client both maintain a globally accessible set of channels that are identifiable through a unique ID (which the channel objects themselves are aware of). This is done through a *ChannelContainer* object. In the case of the Server, the number of active channels varies a lot, whereas the Client has a maximum of three (the "Self" channel, the "Server" channel and the "SP Match" channel).

### Managers

Throughout the entire code base, there's **extensive** use of *Manager* objects. A *Manager* defines a single or multiple behaviors over time and / or in reaction to events that require persistent data. In this codebase, every manager has the possibility of overriding a set of functions :

- Start, Update and End. Pretty self-explanatory : whoever "owns" the *Manager* calls these to start, update (given a deltatime) and "end" (IE deinitialize) the State.
- RegisterMessageReception & UnregisterMessageReception : These have to do with the *Message Dispatching* system. In the code base there exists an extensively used *MessageDispatcher* object type whose role is to store a list of Registered *Message Handlers* that it calls, passing in the *MESSAGE* structure, depending on the kind of registration that handler went for (*Header registration* = React to Messages of a certain Header / range of headers, *All* = React to all Messages, *Channel* = React to all Messages whose metadata indicate they were received on a specific Channel ID). Because *Managers* must NOT know about their owner directly (IE Direct reference to what owns them are forbiden for greatest flexibility) the way they are allowed to react to messages is by being allowed to register and unregister themselves on a Dispatcher object that whatever owns them passes to them using this function.

What *Managers* do around the code base is extremely diverse and there are many layers : *Managers* can be found on the highest level (owned by the global management class like GameClient) just aswell as on much lower levels (Match objects that are themselves usually owned by a manager, have their own Managers !). They're a cornerstone of the "control flow" of the software.

Most of the time *Managers* are not owned directly, but rather through a *ManagerContainer* object so that changing what Managers something contains can be changed just by modifying that thing's construction code rather than its class definition (which I personally consider to be cleaner). When using that container, specific *Manager* objects can be queried using the *GetManager*\<Type\>() function that simply returns the first Manager of that type contained within. If none are found it returns null. This might be the one drawback of this approach : without the specific *Manager* types being present in the class definition, we are not strictly speaking guaranteed that it is, in fact, owned by objects of that class.

### Gameplay

A game using the NeoRTS framework runs its actual "Simulation" gameplay code using a *Match* object. A Match object, upon being fed *Start data* (which, as of now, contains all starting *Objects*), starts simulating a game using a mix of *Managers*, *Workers* and the *Object Memory*. Currently, a Match only ever ends if one of its players disconnects from the server (the match is thus ended externally). It never ends in Singleplayer as for now the only way of leaving the game at that point is to close it entirely : current code is lacking an endogenous end condition.

#### Objects & Memory

In a game ran on the NeoRTS framework, all dynamic elements that are part of the game (think Units, Buildings, Projectiles, Persistent spells, Destructibles...) are interchangeably known as *Entities* or, more commonly (and as in code) : *Objects*.

The problem Objects solve is relatively simple : we need our simulation to update more-or-less independent objects of variable types, data, and behaviors. Especially with a heavily object oriented language like C#, there are many options. Each have their pros and cons. It is tempting to go for a very simple solution where, for example, you would design each of your broad *Object* types by inheriting from an Object class, or by composition through components that would be centralized by an *Object* instance through a list of references.

That solution would work well in many cases, but not here : I had high ambitions for the performances of the game and wanted to support at least 4 figures of dynamic objects being active in a single match at a time. When you want to go that high, you cannot scuff on any major performance concerns, and the one many solution do sacrifice is **Cache locality**, or **Avoiding Cache Misses**.

The fact is that we need our data to be tightly packed together in memory so that as much of it as possible can be loaded together into processor caches and thus give a great speed boost to our object processing throughput. This means the data has to be orginanized in a way that disallows design simplifications and flexibility like the aforementionned object oriented solutions get.

I thus built a design that prioritizes where the data actually ends up in memory and, generally speaking, speed of reading and processing that data, at the expense of flexibility when necessary. This is known as a *Data Centric* design.

*Objects* can "own" *Data Component*, which are simple, usually very small data structures, often pure data containers with no methods or complex constructor. The actual data is laid out inside DataContainer objects, which are simply a wrapper around an array of a specific type of data structure and are able to react to *DataChangeEvents* (Explained later). All *DataContainers* are centralized within the *ObjectMemoryManager*, one instance of which every *Match* object owns.

In order to assign that data to actual *Objects* and generally manage it, *Object* instances turned out to be simple lookup tables. Each *Object* contains two arrays of constant size, of type *uint*. The first contains the *Type ID* (*which is also the ID of the actual container*) of an owned component (*which gets calculated through reflection in alphabetical order, so as to maintain consistency accross machines given they have the same version of the codebase*), and the second contains the corresponding index within the *DataContainer* of the actual type...

Creating a new type of data is extremely easy : access the *Commons* assembly code and copy one of the existing data structures like OBJECT_DATA_HEALTH in order to keep the class attributes (the *ObjectDataTypeID* one is necessary to identify the structure as an *Object Component*). Rename your structure (following the naming pattern of OBJECT_DATA_[WHATEVER ELSE] is recommended but not strictly needed) and give it whatever data you see fit, as long as the structure remains **Unmanaged** (IE no directly or indirectly containing a reference type). Once done it will be automatically detected and used by the *ObjectMemoryManager*, will have a *Type ID* and will thus be assignable to *Objects*. 

Finally, the *Objects* themselves are also contained within a single block of memory within the *ObjectMemoryManager* aswell. Since all are allocated at the start of the Match, and because the max amount of Objects being active at the same time is a known constant, this means we can easily iterate over the *Objects* and their *Owned Data* given that we have access to the *Objects* array and whichever *Container* we want to read from or write into. This is what *Workers* do.

#### Worker system

In order to run the simulation, some of the work is done by *Managers* directly, in a way that was never meant to be multithreaded or otherwise broadly optimized. Manager code can technically achieve anything but it is "trusted" to be relatively lightweight. However, no matter how performant our algorithms are, the sheer amount of work can get phenomenal when a high amount of complex Objects. Multithreading was going to become a necessity eventually (although to this day it still wasn't necessary to add any of it...), and thus the framework needed a design that would make it easy to parallelize as much of the work as possible whenever we got around to doing it.

Introducing : *Workers*. Worker objects represent "units" of work within the *Worker Pipeline* which is the bulk of the work needed to run a single Tick of the simulation. *Workers* are "Singleton" objects at the scale of a *Match* object : although possible, there would be no point creating multiple *Worker* objects of the same type within the same Match. *Workers* are held by the owner Match inside a List, which determines the order in which they run. There are  5 stages in the **Worker Pipeline** :

- Frame Begin : the *Worker* can do work when the Tick / Frame begins. However, it runs before any other stages on any other *Worker* types, meaning work that depends on other *Workers* running first can't be done here. Once all *Workers* have ran their *Frame Begin*, the *Pre Work*->*Work*->*Post Work* stages start.

- Pre Work : the *Worker* can do work before it starts iterating over every *Object*, IE this stage only runs once like the previous one. However, at this point every worker has ran their *Frame Begin* stage, and all *Workers* higher in the list have ran their *Pre Work*, *Work* and *Post Work* stages. This means dependencies on other *Workers* have been "resolved" for this Tick at this point.

- Work : the bulk of the actual work for most *Workers*. Runs once per *Active / Living Object*. On each iteration the *Worker* is given an *Object ID* it can use to query data through a protected access function all *Workers* have called *FetchObjectData(Type, ID)*.  

- Post Work : same as *Pre Work*, except runs after *Work*. Following this stage, the next *Worker* in the list's *Pre Work* begins. If this was the last *Worker* in the list, then every *Worker* proceeds to run the next stage.

- Frame End : Every *Worker* has ran the rest of the pipeline at this point. This can be used for "final cleanup" work to prime the Worker for the next Tick, but before it actually begins.

Note that **every** stage of this pipeline run **after** *Managers* get updated.

The point of this whole system is to be able to add new features in a way that allows extreme granularity in the actual data processing : *Workers* can get quite complex but it is usually not difficult to split them into smaller *Workers*. To help enforce this idea, *Workers* are, in spirit, forbidden from maintaining a direct reference to the *Match* object or to any of its *Managers*. **All data references the *Worker* is going to be reading from or writing into has to be cached on construction, which happens at the start of the *Match*. Usually they need to cache a reference to the underlying array of data owned by *Containers* in the *ObjectMemoryManager*.**

Although currently a single thread executed the whole pipeline for every *Worker* following the List as declared by hand in code, you can probably see how easy it would be to automatically figure out **dependencies** and organize the *Worker* objects into multiple "layers" of parrarel execution. At least, that's how it seems to me in theory. It also allows for neat organization of gameplay features into bite sized code classes that can easily be disabled by simply not creating the Worker during *Match* construction.

Currently *Workers* already do a range of tasks : processing object movement, combat, unit AI, spawning from special buildings, death, target search, unit collision... As an example, it only took me half an hour to implement the latter ! Granted, it is a "dumb" implementation but it could be made to benefit from the same optimization *target search* does, whenever I get around to it.

*Creating* a new worker type from scratch is actually pretty simple : define a new class within the *Commons* assembly that inherits from *Game_Worker_Base*. Satisfy the few requirements of that base class, and then simply override any of the 5 pipeline functions you want your worker to use. If your *Worker* needs to read from or write into any external data (which it likely does...) then fill whatever references to it in through construction parameters which you can then satisfy within the *Match* construction code. You might take notice of the *IObjectDataContainerHolder* interface, which defines the ability of returning a *DataContainer* of any type. This essentially allows passing in the *ObjectMemoryManager* itself into the *Workers*' constructors without breaking the "no direct reference to the *Match* or its *Managers*" rule which simplifies the constructor prototypes greatly.

Usually the references you will need cached inside a *Worker* are simple array references into Data owned by *Containers*, but it can really be anything else. One *Worker* even has a reference to a *Function* so that the code for it could be written inside a *Manager* without breaking the aforementioned rule.

#### Synchronization

At this point you might be wondering how all this gets synchronized during Online play. The main thing to understand about it is that going for what I like to call a "Valve solution" for online synchronization was not a possibility here. As a reminder, here's the main idea behind a "Valve Solution" :

-> Player joins game, receives data from the server about the data they need to load (Map, objects, sounds...)
-> Once the Client has loaded everything it needed, it has been "primed" and is ready to join the State Updated group of players connected to the server. That group of players received a full update of the Game State many times per second (usually featuring the position, rotation, health... of every active object on the server).
-> The Server simulates everything and sends the result to every Client. Clients therefore only have to show the data as best they can, and maybe run some inter/extrapolation to smooth the experience out.

This approach has pros and cons :

Pros :
- More instinctive to implement with an Object-Oriented approach : you have a bunch of objects running some code, and every frame you take their current state and send it to everyone else (if you're the server). Easy !
- Desynchronization is less of an issue the more "complete" each game state update are. You can potentially update *everything* on every tick and be entirely shielded from desynch problems so long as connection persists.
- Since the Client's state only / mostly relies on the Server's latest update, it means joining an ongoing game is very easy to accomplish : just load the (usually static) data the match "started with" (map, gamemode... whatever the server doesn't keep "reminding" the clients about) and then hook the newly connected player to the State Update system.

Cons :
- Very dependent on a strong connection, especially for fast paced games. Since games of that nature are not usually very deterministic (the amount of different things that CAN happen with 60 players each controlling a character with a gun is... pretty high) the client is totally reliant on the server updating it about what's happening EXTREMELY frequently, and any lag will likely mean a worse playing experience.
- **Limited on how complex or big the game can get (and that's the main issue here). Because the server needs to keep sending updates about most if not every object in the game, the amount of data can't get too high otherwise bandwith gets overloaded.**


Because of this last drawback, the Valve approach is not feasible. Imagine a match involving just 4 players each making an army of 200 units. That's a LOT of positions, healths, mana bars, attack states... to update the clients with. The strategy I ended up using for this project is called *Lockstep*.

The idea of *LockStep* is actually relatively simple : assuming the game simulation is FULLY deterministic outside Player input, you end up with the following property.

Take G(T) as the function determining the Game's state at time T. Take G[X](T) as the game's state at time T for client X, or the server for G[S](T).
For clients 1, 2, and server S :

Assuming no player input happens between times T1 and T2, and assuming G\[1\]\(T1\) == G\[2\]\(T1\) == G\[S\]\(T1\), then G\[1\]\(T2\) == G\[2\]\(T2\) == G\[S\]\(T2\).

If any player input happens at time TI (with T1 < TI < T2), assuming it gets taken into account at time TI exactly on every client and server, then the relationship holds true.

This has the implication that, so long as the starting data is the same, then the entire game can be synchronized just by synchronizing player input actions (that by nature cannot be predicted). This means very little data is needed to synchronize even big simulations with a lot of moving parts : let every client & the server do the work instead of overloading the network bandwith is the idea.

The pros & cons are basically the contrary of the Valve approach. On top of that there are three challenges to take on due to the complex nature of this approach :

- The game simulation has to be perfectly deterministic outside player input. This means random number generation is much more difficult to accomplish (as the same number would have to be generated on all ends, and be generated again if we ever need to go back in time for a replay or "catch up" system). In some cases problems as small as float imprecisions can create desynch.
- Any non deterministic events (player input & cheat codes) have to be recorded and synched in a way that makes it be taken into account at the exact same theoretical time on every machine running the simulation.
- If any desynchronization does occur... safe to say trying to restore that game to a working state will be a headache.

In our case making the game simulation fully deterministic on all ends wasn't a great challenge : we're building an RTS so as long as we avoid using randomness in, say, unit AI or combat, we're fine. On both clients and servers the exact same Match code is ran, with the exact same memory management and the exact same Workers. That in itself does not guarantee synchronicity over time of course, since we're lacking an enforced tickrate - meaning slower machines do tend to stray away from what higher end machines simulate over time. But it's a start.

The main challenge was finding a way of easily synchronizing changes in data related to player input. What happens when a player orders some of their units to attack ? This is where *DataChangeEvents* come in.

A *DataChangeEvent* is the expression of a change in data that "wants" to happen. It features the type of data it wants to change (through the same *Type ID* as in the *Object Memory* part), the affected *Object IDs* and the new value for that data type. Later it could also contain a timestamp of when the event was generated on the player's end as opposed to received by the server.

The idea is that the purely Client-Side code generates these events through any means - UI, keybinds, selection system... and sends them to the *Server Match*... and here's where it might get confusing :

- When playing multiplayer, as we'd expect, although we are running our own *Match* object locally, the event gets sent through channel 1 to the *Server*, who receives it, potentially runs sanity checks (to make sure we're not trying to control units we don't own for example), and at the same time "broadcasts" the event back to every client (*including* the one who generated it) so they may also apply it on their ends.

- When playing singleplayer... the event gets sent to the Client itself. Yes, it sends something to itself. The idea is that the Client "gameplay" code such as the *Unit Selection & Control system* shouldn't have to worry about whether the events it generates should be sent to a server or be processed by the *Local Match* directly. When it is the latter, then we "trick" our Client into believing it just received an event to process from the "server" by sending it through channel 0, the "Self" *Communication Channel*.

This explains why there is a "Self" *Channel*. The third *Channel*, "Local Match Channel", exists because sometimes the *Match* object itself generates event messages. To avoid feedback loops of sending -> receiving -> sending as a reaction -> receiving again *ad infinitum* the *Local Match* always sends events generated by itself through a specific channel, whose *received* content it ignores entirely.

And that's for now all there is to say about synchronization in this framework. It's very crude as yet, but it already works well enough for primitive testing and has the advantage of being *extremely* flexible : the *ObjectDataChangeEvent* system can work with **any** type of *Object Data*. In the "Post mortem" section I will write briefly about what I would do to improve it.

\[TO BE CONTINUED\]
