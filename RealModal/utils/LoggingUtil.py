from utils.GlobalVaribles import GlobalVariables as GV


def logging(*args, **kwargs):
    """
    A logging helper function, *arg, **kwargs are directly using by print().
    When GV.debug is set, these logs will be printed for debugging.
    """
    if GV.debug:
        print(*args, **kwargs)
