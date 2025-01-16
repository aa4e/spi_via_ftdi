# SPI via FTDI

## Content

1. Introduction
2. Application features
3. System requirements
4. Installing and configuring the application
5. Working with the application
6. Application status and terms of distribution
7. Contacts
8. Rights
9. Version History


## 1. Introduction

"SPI via FTDI" (SPI via FTDI.exe ) is a software designed to transfer data from a PC and receive data to a PC using the company's chips Future Technology Devices International (FTDI) families FT22xx, FT23xx, FT42xx over serial interfaces SPI, I2C, 1-Wire and MicroWire.

## 2. Application features

- Transfer data from a file or a manually entered array bytes from the master device (FTDI Chip) to the slave device via the SPI, I2C, 1-Wire or uWire interface.
- Receiving data from the slave device via the SPI interface, I2C, 1-Wire and MicroWire.
- Cyclic data reception a set number of times or in an infinite loop.
- Device scanner on I2C and 1-wire buses.
- GPIO pin management.
- Generation of rectangular pulses with a given frequency (in the range from 0 to ~ 5000 Hz) and a duty cycle.
- Frequency meter in the frequency range from 0 to ~5 MHz. 
- Saving the received data to a text or (and) binary file.
- Automatic cleaning of the log when the specified size is exceeded.
- Saving settings (transfer rate, commands, buffer size, etc.) to the profile and restoring the configuration from the profile.
- Creation and execution of scripts - a given sequence of commands.

## 3. System requirements

- Windows 7 and older operating system. The application works with both x86 and x64 operating systems. 
- .NET version 4.6.
- The presence of a USB port.

## 4. Installing and configuring the Application

- It is necessary to make sure that the installed one is available .NET Framework and, in its absence, install it.
- The FTDI CDM WHQL Certified driver must be installed in the system, which can be downloaded from the FTDI Chip manufacturer's website.


## 5. Working with the Application

- The installation of the application is carried out by copying the executable file "SPI via FTDI.exe " from the installation media to the desired location. 
- At startup, the application detects the presence of the necessary libraries and, if they are missing, tries to write them to the system directory. Therefore, it is advisable to perform the first launch with Administrator rights.
- At startup, the application detects the presence of supported FTDI devices connected to the system and creates for each device has its own tab.
- By default, the application is loaded in SPI mode. If you plan to work on the I2C interface, 1-Wire or MicroWire, you need to select the interface via the menu (Device -> Interface). 
- The interface settings are selected by selecting the parameters in the left panel of the main application window. Each device is configured independently.
- The connection to the FTDI device is performed by selecting the "Connect" item in the context menu called on the tab header. Deactivation - by pressing the same button. There is also a duplicate menu (Device -> Enable/disable the current one) and the keyboard shortcut Ctrl+C. 
- Supports simultaneous connection to multiple FTDI devices.
- Data transfer is performed by clicking on the "Write" button. Transfer is possible in three ways:
  - sending a file (a button with a folder icon);
  - sending an arbitrary sequence of bytes (specified in the "Command" field in the 16th form with the checkbox selected);
  - sending a file with a sequence of bytes at the beginning, if the file is selected and the "Command" checkbox is selected. 

- Data is received from the slave device by pressing the "Read" button. Before reading the data, a command can be written to the slave. To do this , activate the "Command" switch and set the command in as an array of bytes in 16-digit representation, separated by spaces or dashes.
- If a number other than 1 is entered in the field to the right of the read or write button, the reception will be performed the appropriate number of times in the cycle. If "0" is specified, then reading or writing will continue indefinitely (until forced to stop). 
- When reading, all received data can be saved to a file (text or binary, depending on the "Reading Options" settings). You can open the recorded files through the "File" menu -> "Open received data". 

## 6. Application status and terms of distribution

The application is used to exchange data with devices implementing SPI, I2C, 1-Wire or MicroWire interfaces. The application is not intended for sale. 

## 7. Contacts

Author: contact@soltau.ru

Website: https://soltau.ru/index.php/themes/dev/item/421-realizatsiya-spi-s-pomoshchyu-mikroskhem-firmy-ftdi

## 8. Copyright

AAVE

## 9. Version History

* *2016:*  SPI and I2C modes.

* *2020:*  1-Wire mode. Scripts (alpha).

* *2021:*  Frequency meter. GPIO control. The I2C bus scanner.

* *2022:*  The frequency generator.

* *2023:*  uWire mode. Improving the operation of the frequency meter. Working with EEPROM.

* *2024:*  Translated into English. Improved work with EEPROM, support for FT_Prog profiles. UART mode.

