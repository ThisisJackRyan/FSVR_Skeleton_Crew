Dissonance LLAPI Demo
=====================

The Demo for the LLAPI is a much more basic demo than for other network integrations. This is because the LLAPI does not include many of the more advanced features required for the standard demo (e.g. player position synchronisation). To see a fully featured demo demonstrating all the capabilities of Dissonance please see the HLAPI demo scene.

See the Dissonance [documentation](https://dissonance.readthedocs.io/en/latest/) for more detailed documentation.


Running the demo
================

As this is a multiplayer voice comms demo, we will want to run multiple clients and connect them all into one session.

1. Add the "LLAPI Demo" scene to your project's build settings and drag it the top of the list.
4. Click File -> Build and Run.
5. Once the client is running, run the Demo scene in the editor.
6. In editor, click "Create Server".
7. On the client, put in the IP address of the server (e.g. `localhost`) and click "Connect To Server"

The demo scene should load on both instances with both players connected.

Global Chat
===========

Global chat is configured via the "Voice Broadcast Trigger" and "Voice Receipt Trigger" behaviours on the DemoWorld entity.

By default, the broadcast trigger is configured to open a channel to the "Global" room via push to talk, on the "TeamChat" input axis (you may need to define this axis in Edit -> Project Settings -> Input). While holding down this button, all players in the session will hear you speak.

Global chat does not use 3D positional audio.


Using LLAPI - Standalone
========================

If your game is using a networking system which Dissonance does not support natively you have two options: write your own network integration, or run LLAPI as a separate network session for Dissonance to communicate. Doing this requires that you have your own system for establishing connectivity (e.g. NAT negotiation).

Using LLAPI - Game Networking
=============================

If your game is using the LLAPI for it's own networking then you can use the Dissonance LLAPI system. Dissonance will host it's own LLAPI session alongside yours.

To ensure that Dissonance does not interfere with your networking find the "UNet Comms Network" component and uncheck "Manage NetworkTransport Lifetime". This setting stops Dissonance attempting to initialize and shutdown the LLAPI network system, doing so becomes your responsibility.