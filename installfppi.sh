#!/bin/bash
NV=/home/pi/erpnet.fp
OV=/home/pi/erpnet.fp.old
SN=erpnet.fp.service
SF=/home/pi/$SN
if [ -d "$NV" ]; then
        echo "Stopping old service..."
        sudo systemctl stop $SN
        sudo systemctl disable $SN
fi
if [ -d "$OV" ]; then
        echo "Cleanup..."
        rm -rf $OV
        mv $NV $OV
fi

echo "Downloading new version..."
wget -q https://github.com/erpnet/ErpNet.FP/releases/latest/download/linux-arm.zip
unzip -qq linux-arm.zip -d $NV
chmod +x $NV/ErpNet.FP.Server

echo "Installing the service..."
echo "[Unit]" > $SF
echo "Description=ErpNet.FP Service" >> $SF
echo "After=network.target" >> $SF
echo "" >> $SF
echo "[Service]" >> $SF
echo "ExecStart=$NV/ErpNet.FP.Server" >> $SF
echo "WorkingDirectory=$NV" >> $SF
echo "StandardOutput=inherit" >> $SF
echo "StandardError=inherit" >> $SF
echo "Restart=always" >> $SF
echo "User=pi" >> $SF
echo "" >> $SF
echo "[Install]" >> $SF
echo "WantedBy=multi-user.target" >> $SF
sudo cp $SF /etc/systemd/system/$SN
sudo systemctl enable $SN
sudo systemctl start $SN

echo "Waiting 30s for printers discovery..."
sleep 30s

fprinter=`curl -s --location --request GET 'http://localhost:8001/printers' | grep -oP '(?<="serialNumber":")[^"]*'`
if [ -z "$fprinter" ]
then
        echo "There are no printers detected"
else
        echo "Setting hostname"
        echo "FP-$fprinter" | sudo tee /etc/hostname
fi
echo "Done."
