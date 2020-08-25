import sys
import unittest
import operator
from functools import reduce

class Diamond(object):
    def print_up_to(self, limit):
        return reduce(
            operator.concat,
            (
                self.build_line(c, limit)
                for c in self.letters_up_to_and_back(limit))
            )

    def letters_up_to_and_back(self, limit):
        letters_from_a_to_limit = range(ord('A'), ord(limit))
        for o in letters_from_a_to_limit:
            yield chr(o)
        yield limit
        for o in reversed(letters_from_a_to_limit):
            yield chr(o)

    def build_line(self, current, limit):
        line = self.indent(current, limit)
        line += current
        line += self.separator(current)
        if current > 'A':
            line += current
        line += "\n"
        return line

    def indent(self, current, limit):
        return self.spaces(ord(limit) - ord(current))

    def spaces(self, count):
        return " " * count

    def separator(self, current):
        distance = ord(current) - ord('A')
        return self.spaces(distance * 2 - 1)


class DiamondTest(unittest.TestCase):
    # def __init__(self, room):
    #     self.room = room
    room = 'DD16'
    path = 'C:\\Users\\rcmurray\\git\\DANCEcollaborative\\bazaar\\DiamondKataPSILegacyAgent\\runtime\\testcases\\'
    suffix = ".txt"

    def test_a(self):
        """" Test input 'A' outputs a single line """
        self.assertEqual("A\n", Diamond().print_up_to('A'))
        filename = self.path + 'room-' + self.room + '-test-a' + self.suffix
        f = open(filename, 'w')
        f.write("test case a passed")
        f.close()

    def test_b(self):
        """" Test input 'B' outputs a diamond """
        self.assertEqual(" A\n"
                         "B B\n"
                         " A\n", Diamond().print_up_to('B'))
        filename = self.path + 'room-' + self.room + '-test-b' + self.suffix
        f = open(filename, 'w')
        f.write("test case b passed")
        f.close()

    def test_c(self):
        """" Test input 'C' outputs a bigger diamond """
        self.assertEqual("  A\n"
                         " B B\n"
                         "C   C\n"
                         " B B\n"
                         "  A\n", Diamond().print_up_to('C'))
        filename = self.path + 'room-' + self.room + '-test-c' + self.suffix
        f = open(filename, 'w')
        f.write("test case c passed")
        f.close()

    def test_d(self):
        """" Test input 'D' outputs an even bigger diamond """
        self.assertEqual("   A\n"
                         "  B B\n"
                         " C   C\n"
                         "D     D\n"
                         " C   C\n"
                         "  B B\n"
                         "   A\n", Diamond().print_up_to('D'))
        filename = self.path + 'room-' + self.room + '-test-d' + self.suffix
        f = open(filename, 'w')
        f.write("test case d passed")
        f.close()

if __name__ == '__main__':
    # room = "DD14"
    def __init__(self, room):
        self.room = sys.argv[1]

    n = len(sys.argv)
    print("Total arguments passed: ", n)

    if (n > 1):
        print("First extra arg: " + sys.argv[1])
        # unittest.main(sys.argv[1])

    unittest.main()

""""    myDiamond = Diamond()
    print(myDiamond.print_up_to('E'))
    myIter = myDiamond.letters_up_to_and_back('Z')
    for item in myIter:
        print(item, end = '')     # Python 3.x
    print("")                     # Python 3.x   """
    #     print item,                 # Python 2.x
    # print ''                        # Python 2.x