class GlobalVariables():
    """
    This is a global class which uses static variables to store hyperparameters and share temporary information between
    other classes
    """
    mapping = {
        "messenger_name_mapping": {},
        "listener_name_mapping": {},
        "processor_name_mapping": {},
        "handler_name_mapping": {},
        "camera_name_mapping": {},
        "running_value_mapping": {}
    }

    @classmethod
    def register_listener(cls, name):
        def wrap(listener_cls):
            cls.mapping["listener_name_mapping"][name] = listener_cls
            return listener_cls

        return wrap

    @classmethod
    def register_messenger(cls, name):
        def wrap(messenger_cls):
            cls.mapping["messenger_name_mapping"][name] = messenger_cls
            return messenger_cls

        return wrap

    @classmethod
    def register_processor(cls, name):
        def wrap(processor_cls):
            cls.mapping["processor_name_mapping"][name] = processor_cls
            return processor_cls

        return wrap

    @classmethod
    def register_handler(cls, name):
        def wrap(handler_cls):
            cls.mapping["handler_name_mapping"][name] = handler_cls
            return handler_cls

        return wrap

    @classmethod
    def register_camera(cls, name):
        def wrap(camera_cls):
            cls.mapping["camera_name_mapping"][name] = camera_cls
            return camera_cls

        return wrap

    @classmethod
    def get_listener_class(cls, name):
        return cls.mapping["listener_name_mapping"].get(name, None)

    @classmethod
    def get_messenger_class(cls, name):
        return cls.mapping["messenger_name_mapping"].get(name, None)

    @classmethod
    def get_processor_class(cls, name):
        return cls.mapping["processor_name_mapping"].get(name, None)

    @classmethod
    def get_handler_class(cls, name):
        return cls.mapping["handler_name_mapping"].get(name, None)

    @classmethod
    def get_camera_class(cls, name):
        return cls.mapping["camera_name_mapping"].get(name, None)

    @classmethod
    def register(cls, name, value):
        path = name.split(".")
        current = cls.mapping["running_value_mapping"]

        for part in path[:-1]:
            if part not in current:
                current[part] = {}
            current = current[part]

        current[path[-1]] = value

    @classmethod
    def unregister(cls, name, default=None):
        path = name.split(".")
        current = cls.mapping["running_value_mapping"]

        for part in path[:-1]:
            if part not in current:
                return default
            current = current[part]
        return current.pop(path[-1], default)

    @classmethod
    def get(cls, name, default=None):
        path = name.split(".")
        current = cls.mapping["running_value_mapping"]

        for part in path[:-1]:
            if part not in current:
                return default
            current = current[part]
        return current.get(path[-1], default)


GV = GlobalVariables()
