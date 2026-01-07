# boot.py -- run on boot-up
import network
import time
import machine
import esp

# Connect to the network

esp.osdebug(None)
wlan = network.WLAN(network.WLAN.STA)
wlan.active(True)

if not wlan.isconnected():
    print('connecting to network...')
    wlan.connect('your-SSID', auth=(network.WLAN.WPA2, 'your-PASSWORD'))
    while not wlan.isconnected():
        time.sleep(1)

print(wlan.ifconfig())