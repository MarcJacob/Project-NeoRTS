# Project-NeoRTS

## Streamed Online RTS Project.

### Link to current Client build : \<Defunct\>

Note that the game isn't in a playable state : all present interactive elements are built as a test, using the Framework beneath as a service of sorts. 
Development was entirely streamed on my Twitch channel : https://www.twitch.tv/Hoshiqua
... and entirely uploaded to my Youtube channel : https://www.youtube.com/channel/UCvdyDp5GVFhEls_3hy7M38A

Because of my break in streaming, and because I'd probably start again from scratch anyway, development of this framework is suspended.

If you want a breakdown of the codebase, check out the [Wiki](https://github.com/MarcJacob/Project-NeoRTS/wiki/).

## Will I continue this project? What would I do better if starting from scratch?

Before ending this document, I wanted to write about the future of this project, or rather why I won't keep working on this code base.

This was entirely developed on stream and uploaded to YouTube. This means that this project only continued so long as I felt like it made for good content - which I eventually decided it did not (I wouldn't recommend going for long winded projects as Twitch content as the more you advance the less welcoming it is to newcomers).

Beyond that, I think this project has already made me learn many new things and make me fiddle with nearly every aspects of game development a Unity Developer might have to fiddle with (with a few exceptions like advanced animating, shader development and net security). So stopping now, I still feel like this project was far from being a waste of time even when taken outside the context of streaming it.

Finally, even so early in development I already feel like I made a few mistakes that would justify starting from scratch if I ever wanted another go at making an online RTS framework:

-> The "Framework" nature of the code didn't become apparent right away: I originally set out to make a *game* and ended up making a *framework* first because of my rule to not use third party libraries. However, the two natures of the code I was writing conflicted, and now the "*framework*" I've produced isn't actually very usable as a framework, as you need to tangle with the actual *Commons* to change even very basic things, whereas a good framework in C# should be fully usable without changing its core code. For example, starting again, I would make sure to be able to add *Workers* and *Data Components* seamlessly from outside the *Commons* assembly. 

-> Because, in part, of my own ignorance of what C# can do (IE I didn't know about the BinaryWriter/Reader objects) and my awakening to C++ happening in parallel, there is a lot of *pointer code* (or *unsafe* code as it's called in C#), sometimes for reasons of performance, other times for encoding / decoding messages. In the meantime, I learned to write C++ native dlls and starting again I would probably write the bulk of the *Commons* as a C++ native library (especially performance critical code like the *Match Code*), with the C# part being a wrapper for usability by users of the framework.

-> Finally, simply put: there was a complete lack of a longer term plan. No *Trello* or *Hack'n plan* board, no commit discipline... This means that a lot of time was spent working on frankly non important features and wondering what exactly to do next. I would also have liked to present the project over time in a more accessible way than hours long VODs on YouTube.

Thanks for reading! I will continue streamed development here and there on my Twitch channel (https://www.twitch.tv/Hoshiqua), with "one-off" series where I try to "solve" a certain problem on much smaller projects that get worked on for only a handful of streams.
