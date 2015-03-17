#coding:utf-8
import os
import sys
import subprocess
import shutil

def execute(cmd):
    """コマンドを実行"""
    subprocess.call(cmd, shell=True)
    print cmd

def talk():        
    cmd = "open_jtalk -m voice_bank/mei_happy.htsvoice -x /usr/local/dic/ -ow talk.wav talk.txt"
    execute(cmd)

    cmd = "aplay talk.wav"
    execute(cmd)

