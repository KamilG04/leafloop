const path = require('path');

module.exports = {
    entry: {
        // Services
        api: './wwwroot/js/services/api.js',

        // Existing Components
        itemList: './wwwroot/js/components/itemList.js',
        myItemList: './wwwroot/js/components/MyItemList.js',
        itemDetails: './wwwroot/js/components/itemDetails.js',
        itemCreateForm: './wwwroot/js/components/itemCreateForm.js',
        itemEditForm: './wwwroot/js/components/itemEditForm.js',
        userProfile: './wwwroot/js/components/userProfile.js',
        myTransactionsList: './wwwroot/js/components/MyTransactionsList.js',
        transactionDetails: './wwwroot/js/components/TransactionDetails.js',
        profileEditForm: './wwwroot/js/components/ProfileEditForm.js',
        initNearbyItemsPage: './wwwroot/js/components/initNearbyItemsPage.js',

        // Events - TYLKO główny entry point
        eventsPage: './wwwroot/js/components/EventsPage.js',

        // Location Components  
        locationPicker: './wwwroot/js/components/locationPicker.js',
        userLocationMap: './wwwroot/js/components/userLocationMap.js',
        nearbyItems: './wwwroot/js/components/nearbyItems.js',
    },
    output: {
        path: path.resolve(__dirname, 'wwwroot/js/dist'),
        filename: '[name].bundle.js',
        clean: true,
        assetModuleFilename: 'images/[hash][ext][query]'
    },
    module: {
        rules: [
            {
                test: /\.(js|jsx)$/,
                exclude: /node_modules/,
                use: {
                    loader: 'babel-loader',
                    options: {
                        presets: [
                            '@babel/preset-env',
                            ['@babel/preset-react', {runtime: 'automatic'}]
                        ]
                    }
                }
            },
            {
                test: /\.css$/,
                use: [
                    'style-loader',
                    'css-loader'
                ]
            },
            {
                test: /\.(png|svg|jpg|jpeg|gif)$/i,
                type: 'asset/resource',
            }
        ]
    },
    resolve: {
        extensions: ['.js', '.jsx']
    },
    // Dodaj devtool dla lepszego debugowania
    devtool: 'source-map'
};