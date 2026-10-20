"""
Makes a password hash for AdminAuth.AdminPasswordHash (TreeRecycle.Web/Code/AdminPage.cs).
Same format Passwords.Hash produces in C#: v1.{iterations}.{salt b64}.{PBKDF2-SHA256 b64}

    python -I tools/hash_password.py            # prompts for the password (not echoed)
    python -I tools/hash_password.py --generate # makes a strong random password and prints both
"""
import base64
import getpass
import hashlib
import os
import secrets
import string
import sys

ITERATIONS = 120000


def make_hash(password):
    salt = os.urandom(16)
    dk = hashlib.pbkdf2_hmac("sha256", password.encode("utf-8"), salt, ITERATIONS, 32)
    return "v1.%d.%s.%s" % (ITERATIONS, base64.b64encode(salt).decode(), base64.b64encode(dk).decode())


def main():
    if "--generate" in sys.argv:
        alphabet = string.ascii_letters + string.digits + "!#%+-=?@^_~"
        password = "".join(secrets.choice(alphabet) for _ in range(28))
        print("Password: " + password)
    else:
        password = getpass.getpass("Admin password: ")
        if len(password) < 16:
            sys.exit("Use at least 16 characters.")
    print("Hash:     " + make_hash(password))


if __name__ == "__main__":
    main()
