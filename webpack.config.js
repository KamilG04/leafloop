// webpack.config.js
const path = require('path');

module.exports = {
    entry: {
        // React components
        userProfile: './wwwroot/js/react/userProfile.js',
        itemList: './wwwroot/js/components/itemList.js',
        itemDetails: './wwwroot/js/components/itemDetails.js',
        itemCreateForm: './wwwroot/js/components/itemCreateForm.js',
        itemEditForm: './wwwroot/js/components/itemEditForm.js',
        myItemList: './wwwroot/js/components/MyItemList.js',
        darkModeToggle: './wwwroot/js/components/darkModeToggle.js',
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
        extensions: ['.js', '.jsx'],
        alias: {
            '@utils': path.resolve(__dirname, 'wwwroot/js/utils'),
            '@components': path.resolve(__dirname, 'wwwroot/js/components')
        }
    },
    optimization: {
        splitChunks: {
            chunks: 'all',
            cacheGroups: {
                vendors: {
                    test: /[\\/]node_modules[\\/](react|react-dom)[\\/]/,
                    name: 'react-vendors',
                    chunks: 'all',
                    priority: 10
                },
                commons: {
                    name: 'commons',
                    minChunks: 2,
                    priority: 5
                }
            }
        }
    },
    devtool: process.env.NODE_ENV === 'development' ? 'source-map' : false
};