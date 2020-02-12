import sys
import random


def get_login_info(uid):
    return "User %05d logged in" % uid


def get_logout_info(uid):
    return "User %05d logged out" % uid


def gen_sorted_login_info(num_users):
    for i in range(num_users):
        yield get_login_info(i)


def gen_sorted_logout_info(num_users):
    for i in range(num_users):
        yield get_logout_info(i)


def gen_random_login_info(num_users, num_lines):
    for _ in range(num_lines):
        uid = random.randint(0, num_users - 1)
        yield get_login_info(uid)


def main(args):
    if len(args) != 2: raise Exception
    num_users, num_lines = int(args[0]), int(args[1])
    with open("test.in.txt", "w") as file:
        for line in gen_sorted_login_info(num_users):
            file.write(line + "\n")
        for line in gen_random_login_info(num_users, num_lines):
            file.write(line + "\n")
        for line in gen_sorted_logout_info(num_users):
            file.write(line + "\n")


if __name__ == "__main__":
    main(sys.argv[1:])