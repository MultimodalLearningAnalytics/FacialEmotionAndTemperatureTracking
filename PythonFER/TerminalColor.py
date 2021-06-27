from enum import Enum


class TerminalColor(Enum):
    FAIL = 'red'
    SUCCESS = 'green'
    WARNING = 'yellow'
    INFO = 'blue'