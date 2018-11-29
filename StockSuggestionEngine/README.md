#Launch the App
To use the Stock Suggestion Engine, navigate to the publish directory and run the StockSuggestionEngine.exe.
The application takes one CLI argument - long. If the argument is anything besides long it will use the short descriptions to perform the text ranking analysis. Using the long argument makes the application use the long description for the text ranking analysis. the long argument will make the application run slower, but generally will provide much more accurate results.

#Using the App
The app will prompt you to input a company's stock ticker. First it will print out some information about the company you input.
Then you will see some output that says "Initializing Query Vector" - this means it is starting the text rank analysis. The program will run for some time, generally 20-45 seconds. It will then print out 20 results and their similarity scores. These are the top 20 companies in our dataset that are most similar to the company you input.