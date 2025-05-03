const path = require('path');

module.exports = {
    entry: {
        // Services
        api: './wwwroot/js/services/api.js',

        // Components
        itemList: './wwwroot/js/components/itemList.js',
        myItemList: './wwwroot/js/components/MyItemList.js',
        itemDetails: './wwwroot/js/components/itemDetails.js',
        itemCreateForm: './wwwroot/js/components/itemCreateForm.js',
        itemEditForm: './wwwroot/js/components/itemEditForm.js',
        userProfile: './wwwroot/js/components/userProfile.js',
        myTransactionsList: './wwwroot/js/components/MyTransactionsList.js',
    },
    output: {
        path: path.resolve(__dirname, 'wwwroot/js/dist'),
        filename: '[name].bundle.js',
        clean: true
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
            }
        ]
    },
    resolve: {
        extensions: ['.js', '.jsx']
    }
};