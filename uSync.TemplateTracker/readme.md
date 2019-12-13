## uSync Template Tracker (Experimental)

Experimenting with tracking file changes in the view folder, 
and automagically creating the templates in umbraco when you 
create a view 

### Progress
- [x] find views that don't have a template in umbraco
- [x] calculate the parent from the layout attribute
- [x] create the template in the right place 
- [x] check at startup
- [ ] run a filewatcher for file changes while project is running
- [ ] handle movement of templates (change the layout changes the parent)

### Notes
- this may or may not be practical. 
The main issue is how filewatchers can trigger 'alot' so we will need
to keep the chatter down.