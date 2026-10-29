## uSync ui library. 

The uSync UI library ,is used by other uSync packages, to 
show things like changes, and updates ,etc. 

> [!IMPORTANT]
> Most packages do not need to export thier client library
as a npm package, unless your client library is also going
to be used by child packages.

### SignalR negotiation

The uSync SignalR client respects `Umbraco:Cms:SignalR:ClientShouldSkipNegotiation`
when exposed by Umbraco's server context. When enabled, the client connects using
WebSockets without negotiation. When disabled, or on older Umbraco versions
that do not expose the setting, negotiation and transport fallback remain enabled.
