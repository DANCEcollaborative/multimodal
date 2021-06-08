from DialogAgents.SolitaireAgent import SolitaireAgent
from DialogAgents.FileReaderAgent import FileReaderAgent

_agent = {
    "Solitaire": SolitaireAgent,
    "FileReader": FileReaderAgent,
}


def agent_build_helper(*args, **kwargs):
    agent = _agent[kwargs["agent_type"]]
    kwargs.pop("agent_type")
    return agent(**kwargs)
