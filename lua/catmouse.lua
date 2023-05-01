local catMouseEvent = ac.OnlineEvent({
    key = ac.StructItem.key('catMouseTeleport'),  -- to make sure there would be no collisions with other events, it�s a good idea to use a unique key
    rotation = ac.StructItem.vec3(),
    status = ac.StructItem.int32(),
    index = ac.StructItem.int32()
}, function (sender, message)
    
    status = message.status
    ac.debug("index", message.index)
    ac.debug("locked", message.status)

    if message.index == ac.getCar(0).sessionID then
        if status == 1 then
            local destination = vec3(156.78, -1.1, -14.33)
            physics.setCarPosition(0, destination, message.rotation)
            physics.setCarVelocity(0, vec3(0, 0, 0))
            physics.engageGear(0, 0)
        end
        if status == 2 then
            physics.setCarVelocity(0, vec3(100, 0, 0))
            physics.engageGear(0, 0)
        end
    end
end)



local function triggerCatMouseEvent()
	catMouseEvent({

    })
end


ui.registerOnlineExtra(ui.Icons.FastForward, 'CATMOUSE', nil, triggerCatMouseEvent, nil, ui.OnlineExtraFlags.None)
