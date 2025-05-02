const path = require('path');

module.exports = {
    // Mode will be set by CLI arguments in package.json
    entry: {
        // React components
        userProfile: './wwwroot/js/react/userProfile.js',
        itemList: './wwwroot/js/components/itemList.js',
        itemDetails: './wwwroot/js/components/itemDetails.js',
        itemCreateForm: './wwwroot/js/components/itemCreateForm.js',
        itemEditForm: './wwwroot/js/components/itemEditForm.js',
        myItemList: './wwwroot/js/components/MyItemList.js',
        darkModeToggle: './wwwroot/js/components/darkModeToggle.js',

        // Add other entry points as needed
    },
    output: {
        path: path.resolve(__dirname, 'wwwroot/js/dist'),
        filename: '[name].bundle.js',
        clean: true // Clean output directory before each build
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
                            ['@babel/preset-react', {runtime: 'automatic'}] // Automatic import of React
                        ],
                        plugins: [
                            // Add any Babel plugins here
                        ]
                    }
                }
            }
        ]
    },
    resolve: {
        extensions: ['.js', '.jsx'], // Allows importing without specifying extension
        alias: {
            // Add aliases if needed
            '@utils': path.resolve(__dirname, 'wwwroot/js/utils'),
            '@components': path.resolve(__dirname, 'wwwroot/js/components')
        }
    },
    optimization: {
        // Extract common dependencies into shared chunks
        splitChunks: {
            chunks: 'all',
            name: 'vendors',
            cacheGroups: {
                // Extract React and ReactDOM into a separate vendor chunk
                vendors: {
                    test: /[\\/]node_modules[\\/](react|react-dom)[\\/]/,
                    name: 'react-vendors',
                    chunks: 'all',
                    priority: 10
                },
                // Common code used across multiple chunks
                commons: {
                    name: 'commons',
                    minChunks: 2, // Minimum number of chunks that must share a module
                    priority: 5
                }
            }
        }
    },
    // Development tools
    devtool: process.env.NODE_ENV === 'development' ? 'source-map' : false
};