# MapleGlobal (WvsBeta_REVAMP)

This is it. The last version before closing down.

This only contains the server source, without the scripts and other 'private' things, making
it incompatible with the client. Good luck fixing that.

## Notes
- We will not give support on this release. This file has all the support we want to give.
- The NX files are from some old version I had lying around. Possibly not fully compatible with client.
- Config options for Redis, ElasticSearch/logging and some others are missing
- ReNX has been modified to support merging of NX files
- log4net.ElasticSearch was made a bit better in terms of memory usage and logging
- SQLs/wvsbeta_sql_compatible.sql should be compatible with the server
- packages/ folder contains the NuGet packages.


## Features
- Lots.
- log4net and ElasticSearch for logging/tracing
- Redis for locking character transfers when they CC and other features
- NX data files
- Threaded packet sending queue to offload slow sockets
- PatchCreator for maple .patch files, using jdiff
- Improved MasterThread for running the best as possible, without CPU load
- Anti-Hack measures
- Discord webhook support
- bcrypt password hashing
- NO NPC SCRIPTS INCLUDED

## Credits
- wackyracer
- Exile
- Diamondo25
- Sifl
- Anthony
- SpAgentMoo
- Joob
- Rath
- Ginseng
- csproj (for doing about nothing at all. hurr)

And all staff and the players that have made the server a great success. You'll be remembered.

## Things we've learned
- Don't exploit or hack. We knew all along.
- Don't bug GiveEXP function so that it saves on every exp gain, while there are 50 people training (thanks Exile :) )
- Don't livestream with your camera pointing to your keyboard and then log in.
- Let other people check your code to remove exploits.
- Don't rush releases.
- Delaying releases because migration logic is borked is not helping the problem, either. Like the teleport bug, hurr.
- And if you decide to release and do your last push to git, your git host could drop out (Gitlab issue @ 2017-08-24 at 2 AM)
- KitterzPE or RiPE will not work. Stop crashing your clients...
- Keep your Windows Server up-to-date, otherwise you might get rekt >_>. (We never had that issue)
- There are VPSs running on old hardware out there. Ours was on a Nehalem or Westmere based host.
- Make sure your server host(s) is/are paid. Otherwise you get downtime out of nowhere.
- Don't get top ranked on Google for "maplestory global"
- GTOP is shit. Don't spend money and time into that.
- If you do decide to use GTOP, expect downtimes. And log _everything_ related to it.
- Don't boost your friends as a server owner, outside of Tespia. People are not going to like that. #boostgate
- People like being midget.
- Keep love outside of private servers, or you might get f*cked
- Keep your password private. We've had 5 people that we had to recover from some brother or sister deleting their character(s)...
- Do not push to production directly. That single line edit could have a lot of issues
- Use debug logging using debug logging functionalities, especially when you do it in the attack handler
- Trace.WriteLine is erased. Do not pass a function call, such as packet.ReadInt() to it, because that will disappear.
- Do NOT select text inside a console. It freezes the application running inside it.
- Double check, no, TRIPLE check your queries. Don't want your shoes to end up on your head ;')
- Have a way to test someones character to figure out issues.
- Do not put too much money in Facebook. Your reach will be capped at a certain point
- Use /map 0 for free meso
- Stay away from El Nath, it might kill you until you are dead (and lost all your EXP). Who needs to check HP anyway?
- When you think you fixed an issue with falling down the map, and you accidentally made a teleport cheat on the map boundaries. Which then keeps on existing for weeks.
- TomBradyProtect, and you'll never get a wz editor ever again.
- Don't give away Ilbis as a CM.
- Muting people might make them use emoji responses to talk, instead.
- Make sure repeated actions are in sync, and don't decide to stack up in events due to locking and such hurr...
