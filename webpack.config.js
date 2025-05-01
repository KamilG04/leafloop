// Pełna ścieżka: webpack.config.js (ZAKTUALIZOWANY)
const path = require('path');

module.exports = {
    // Tryb można ustawić tutaj lub przez argumenty CLI (jak w package.json)
    // mode: 'development', // lub 'production'
    // devtool: 'inline-source-map', // Pomocne przy debugowaniu w trybie development

    entry: {
        // Istniejący punkt wejścia:
        userProfile: './wwwroot/js/react/userProfile.jsx',

        // NOWE punkty wejścia dla komponentów przedmiotów:
        // Klucz (np. 'itemList') zostanie użyty jako nazwa pliku wynikowego ([name].bundle.js)
        itemList: './wwwroot/js/components/itemList.js',
        itemDetails: './wwwroot/js/components/itemDetails.js',
        itemCreateForm: './wwwroot/js/components/itemCreateForm.js',
        // Webpack automatycznie dołączy 'auth.js', ponieważ jest importowany w tych plikach.
        // Dołączy również React i ReactDOM z node_modules.
        myItemList: './wwwroot/js/components/MyItemList.js', // lub .jsx
    },
    output: {
        path: path.resolve(__dirname, 'wwwroot/js/dist'), // Katalog wyjściowy
        filename: '[name].bundle.js', // Nazwa pliku wynikowego, np. itemList.bundle.js
        clean: true // Czyści folder /dist przed każdym buildem (zalecane)
    },
    module: {
        rules: [
            {
                test: /\.(js|jsx)$/, // Przetwarzaj pliki .js i .jsx
                exclude: /node_modules/, // Wyklucz folder node_modules
                use: {
                    loader: 'babel-loader', // Użyj Babel Loader
                    options: {
                        presets: [
                            '@babel/preset-env',  // Transpiluj nowoczesny JS do starszych wersji
                            '@babel/preset-react' // Transpiluj JSX i funkcje Reacta
                        ]
                    }
                }
            }
            // Tutaj można dodać reguły dla CSS/SCSS, obrazków itp., jeśli potrzebujesz
        ]
    },
    resolve: {
        extensions: ['.js', '.jsx'] // Pozwala importować pliki bez podawania rozszerzenia
    }
    // Można dodać optymalizacje dla trybu production, np. minimalizację kodu
    // optimization: { minimize: true }
};