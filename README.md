# BBCIngest

Poll the web for the latest edition of an audio file.

## this is how it works

1. When it first runs, if necessary it creates the archive, publish and log folders specified in the settings.
2. It then calculates the publication time of the next edition from the hour and minute patterns in the settings
3. It then goes to sleep until a few minutes before the next edition is due, set by the minutes before setting
4. Then it fetches the previous edition and publishes it as though it were the next edition. This ensures a recent edition will be broadcast
5. It then polls using an http HEAD request every 10 seconds for the new edition
6. Once the new edition is available, it downloads it using http and publishes it, overwriting the file published in 4 above.
7. It then goes to sleep until the due time – this makes the sleep next calculation simple
8. It then repeats from step 2 above.

If the new edition is late, the programme will continue polling after the due time for the number of minutes in the settings "broadcast minutes after.
This defaults to zero but if, for example the station plans to broadcast an edition published at 16:30 at 16:45 it can set this to 15 and if the
new edition is 10 minutes late it will still be fetched and published.

Download and error information is written to a file in the log directory.

## Other settings:

The file fetched is determined by the prefix, basename, webdate and suffix settings. The webdate setting is a .Net DateTime format.
The suffix is probably mp3 or wav.

The file published is set by the, publish, basename, discdate, useLocaltime and suffix settings. The discdate is a .Net DateTime format.
If useLocalTime is false the published file will contain the due date in UTC.
If useLocalTime is true the published file will contain the due date in the PC's local timezone.

The city and station settings only affect text writting to the log file.
