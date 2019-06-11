          _____                  
   __  __/ ___/__  ______  _____ 
  / / / /\__ \/ / / / __ \/ ___/ 
 / /_/ /___/ / /_/ / / / / /__ 
 \__,_//____/\__, /_/ /_/\___/ 🛴
----------- /____/ --- 8.x -------

Welcome to uSync 8 for Umbraco 8

I be not for Umbraco 7 :

  uSync 8 works only on Umbraco 8.x - if you have just installed
  it on a Umbraco 7.x, you should uninstall now and go get a v4.x 
  version.

uSync 8 - is not just an upgrade of uSync

 uSync deals with some fairly base stuff inside Umbraco and when
 Umbraco changes uSync has to change too.
 
 We have taken this opportunity to clean up some of the code in
 uSync - which means underneath almost everything has changed, 
 but we think this means we are more stable and changes should 
 be more reliable. 

 Operationally it's very similar to uSync 4.x, but the files are
 not 100% compatible so you can't upgrade a Umbraco 7 site to
 Umbraco 8 with uSync (for now).

 The action file is gone, now when you delete or rename something
 we create overwrite the uSync file for that item with one telling
 us it was a delete/rename. this makes deployment much nicer - and
 less likely to break. 


Feedback and Join in: https://github.com/KevinJump/uSync8
